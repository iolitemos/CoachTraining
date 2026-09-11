using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Globalization;

namespace CoachTraining.Api.Services;

/// <summary>Athlete Management (requirement.md 4.3, todo.md 4.2).</summary>
public class AthleteService : IAthleteService
{
    private const int SearchResultLimit = 20;
    private const int HeaderRow = 3;
    private const int FirstDataRow = 4;
    private const int LastTemplateRow = 1003;
    private static readonly string[] ImportHeaders =
    [
        "รหัสนักกีฬา*", "ประเภทนักกีฬา*", "ชื่อ-นามสกุล*", "ชื่อเล่น", "วันเกิด", "ปีเกิด",
        "เบอร์ติดต่อ", "ชื่อผู้ปกครอง", "เบอร์ติดต่อผู้ปกครอง", "ระดับนักกีฬา", "วันที่เข้าร่วม", "จังหวัด", "หมายเหตุ"
    ];

    private readonly ApplicationDbContext _db;
    private readonly ILogger<AthleteService> _logger;

    public AthleteService(ApplicationDbContext db, ILogger<AthleteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResponse<AthleteListItemDto>> ListAsync(PagedRequest request, AthleteType athleteType, int? age = null)
    {
        var query = _db.Athletes.Where(a => a.AthleteType == athleteType);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(a =>
                a.AthleteCode.ToUpper().Contains(search) ||
                a.FullName.ToUpper().Contains(search) ||
                (a.Nickname != null && a.Nickname.ToUpper().Contains(search)) ||
                (a.Province != null && a.Province.ToUpper().Contains(search)));
        }

        if (age is not null)
        {
            var targetBirthYear = DateTime.UtcNow.Year - age.Value;
            query = query.Where(a => (a.BirthYear ?? (a.DateOfBirth == null ? null : a.DateOfBirth.Value.Year)) == targetBirthYear);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(a => a.AthleteCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => MapToListItem(a))
            .ToListAsync();

        return new PagedResponse<AthleteListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AthleteDetailDto?> GetByIdAsync(int athleteId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        return athlete is null ? null : MapToDetail(athlete);
    }

    public async Task<(AthleteDetailDto? Result, string? Error)> CreateAsync(AthleteCreateDto dto, int actionByUserId)
    {
        try
        {
            var birthYearError = ValidateBirthYear(dto.BirthYear, dto.DateOfBirth);
            if (birthYearError is not null) return (null, birthYearError);

            var codeTaken = await _db.Athletes.AnyAsync(a => a.AthleteCode == dto.AthleteCode);
            if (codeTaken)
            {
                return (null, "รหัสนักกีฬานี้มีอยู่ในระบบแล้ว");
            }

            var athlete = new Athlete
            {
                AthleteCode = dto.AthleteCode,
                AthleteType = dto.AthleteType,
                FullName = dto.FullName,
                Nickname = dto.Nickname,
                DateOfBirth = dto.DateOfBirth,
                BirthYear = dto.BirthYear ?? dto.DateOfBirth?.Year,
                PhoneNumber = dto.PhoneNumber,
                ParentName = dto.ParentName,
                ParentPhoneNumber = dto.ParentPhoneNumber,
                AthleteLevel = dto.AthleteLevel,
                JoinDate = dto.JoinDate,
                Province = dto.Province,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };

            _db.Athletes.Add(athlete);
            await _db.SaveChangesAsync();

            return (MapToDetail(athlete), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create athlete. Controller: AthletesController Service: AthleteService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<(AthleteDetailDto? Result, string? Error)> UpdateAsync(int athleteId, AthleteUpdateDto dto, int actionByUserId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        if (athlete is null)
        {
            return (null, null);
        }
        var birthYearError = ValidateBirthYear(dto.BirthYear, dto.DateOfBirth);
        if (birthYearError is not null) return (null, birthYearError);

        // Historical integrity (CLAUDE.md 4.5): editing profile fields never
        // touches past Attendance/PrivateSessionAthlete rows — those keep their
        // own snapshot (AthleteCodeSnapshot/AthleteNameSnapshot) taken at the time.
        athlete.FullName = dto.FullName;
        athlete.AthleteType = dto.AthleteType;
        athlete.Nickname = dto.Nickname;
        athlete.DateOfBirth = dto.DateOfBirth;
        athlete.BirthYear = dto.BirthYear ?? dto.DateOfBirth?.Year;
        athlete.PhoneNumber = dto.PhoneNumber;
        athlete.ParentName = dto.ParentName;
        athlete.ParentPhoneNumber = dto.ParentPhoneNumber;
        athlete.AthleteLevel = dto.AthleteLevel;
        athlete.JoinDate = dto.JoinDate;
        athlete.Province = dto.Province;
        athlete.Remarks = dto.Remarks;
        athlete.UpdatedByUserId = actionByUserId;
        athlete.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (MapToDetail(athlete), null);
    }

    public async Task<bool> SetStatusAsync(int athleteId, bool isActive, int actionByUserId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        if (athlete is null)
        {
            return false;
        }

        // FR-ATHLETE-003: deactivating never deletes or alters historical attendance records.
        athlete.IsActive = isActive;
        athlete.UpdatedByUserId = actionByUserId;
        athlete.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public byte[] CreateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("รายชื่อนักกีฬา");
        sheet.ShowGridLines = false;

        sheet.Cell("A1").Value = "เทมเพลตนำเข้ารายชื่อนักกีฬา";
        sheet.Range("A1:M1").Merge();
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 14;
        sheet.Cell("A1").Style.Font.FontColor = XLColor.White;
        sheet.Cell("A1").Style.Fill.SetBackgroundColor(XLColor.FromHtml("#047857"));
        sheet.Cell("A2").Value = "กรอกข้อมูลตั้งแต่แถวที่ 4 ช่องที่มี * จำเป็นต้องกรอก วันที่ใช้รูปแบบ วว/ดด/ปปปป (ค.ศ.)";
        sheet.Range("A2:M2").Merge();
        sheet.Cell("A2").Style.Font.Italic = true;
        sheet.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#475569");

        for (var column = 1; column <= ImportHeaders.Length; column++)
        {
            sheet.Cell(HeaderRow, column).Value = ImportHeaders[column - 1];
        }

        var header = sheet.Range(HeaderRow, 1, HeaderRow, ImportHeaders.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#059669"));
        header.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        header.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

        sheet.Range(FirstDataRow, 1, LastTemplateRow, 1).Style.NumberFormat.Format = "@";
        sheet.Range(FirstDataRow, 6, LastTemplateRow, 6).Style.NumberFormat.Format = "0";
        sheet.Range(FirstDataRow, 7, LastTemplateRow, 7).Style.NumberFormat.Format = "@";
        sheet.Range(FirstDataRow, 9, LastTemplateRow, 9).Style.NumberFormat.Format = "@";
        sheet.Range(FirstDataRow, 5, LastTemplateRow, 5).Style.DateFormat.Format = "dd/mm/yyyy";
        sheet.Range(FirstDataRow, 11, LastTemplateRow, 11).Style.DateFormat.Format = "dd/mm/yyyy";
        sheet.Range(FirstDataRow, 2, LastTemplateRow, 2).CreateDataValidation().List("\"นักกีฬาในสังกัด,นักกีฬาทั่วไป\"");
        sheet.SheetView.FreezeRows(HeaderRow);
        sheet.Columns().AdjustToContents(1, FirstDataRow);
        foreach (var column in sheet.Columns(1, ImportHeaders.Length))
        {
            column.Width = Math.Min(Math.Max(column.Width + 2, 14), 28);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public async Task<byte[]> CreateUpdateTemplateAsync()
    {
        using var source = new MemoryStream(CreateImportTemplate());
        using var workbook = new XLWorkbook(source);
        var sheet = workbook.Worksheet("รายชื่อนักกีฬา");
        sheet.Cell("A1").Value = "แบบฟอร์มอัปเดตข้อมูลนักกีฬาหลายคน";
        sheet.Cell("A2").Value = "แก้ไขข้อมูลที่ต้องการแล้วนำไฟล์กลับเข้า ระบบใช้รหัสนักกีฬาเพื่อจับคู่และจะไม่เปลี่ยนรหัสหรือสถานะ";

        var athletes = await _db.Athletes.AsNoTracking().OrderBy(a => a.AthleteCode).ToListAsync();
        for (var index = 0; index < athletes.Count; index++)
        {
            var athlete = athletes[index];
            var row = FirstDataRow + index;
            sheet.Cell(row, 1).Value = athlete.AthleteCode;
            sheet.Cell(row, 2).Value = athlete.AthleteType == AthleteType.Affiliated ? "นักกีฬาในสังกัด" : "นักกีฬาทั่วไป";
            sheet.Cell(row, 3).Value = athlete.FullName;
            sheet.Cell(row, 4).Value = athlete.Nickname ?? string.Empty;
            if (athlete.DateOfBirth is not null) sheet.Cell(row, 5).Value = athlete.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
            if (athlete.BirthYear is not null) sheet.Cell(row, 6).Value = athlete.BirthYear.Value;
            sheet.Cell(row, 7).Value = athlete.PhoneNumber ?? string.Empty;
            sheet.Cell(row, 8).Value = athlete.ParentName ?? string.Empty;
            sheet.Cell(row, 9).Value = athlete.ParentPhoneNumber ?? string.Empty;
            sheet.Cell(row, 10).Value = athlete.AthleteLevel ?? string.Empty;
            if (athlete.JoinDate is not null) sheet.Cell(row, 11).Value = athlete.JoinDate.Value.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 12).Value = athlete.Province ?? string.Empty;
            sheet.Cell(row, 13).Value = athlete.Remarks ?? string.Empty;
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }

    public async Task<AthleteImportResultDto> ImportAsync(Stream fileStream, int actionByUserId)
    {
        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
            {
                throw new AthleteImportValidationException([Error(0, "file", "ไม่พบแผ่นงานในไฟล์")]);
            }

            var errors = ValidateHeaders(sheet);
            if (errors.Count > 0)
            {
                throw new AthleteImportValidationException(errors);
            }

            var rows = new List<(int Row, Athlete Athlete)>();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? HeaderRow;
            for (var rowNumber = FirstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var cells = Enumerable.Range(1, ImportHeaders.Length)
                    .Select(column => sheet.Cell(rowNumber, column).GetFormattedString().Trim())
                    .ToArray();
                if (cells.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var athlete = ParseRow(sheet.Row(rowNumber), rowNumber, actionByUserId, errors);
                if (athlete is not null)
                {
                    rows.Add((rowNumber, athlete));
                }
            }

            if (rows.Count == 0 && errors.Count == 0)
            {
                errors.Add(Error(0, "file", "ไม่พบข้อมูลนักกีฬาสำหรับนำเข้า"));
            }

            foreach (var duplicate in rows.GroupBy(x => x.Athlete.AthleteCode, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            {
                foreach (var row in duplicate)
                {
                    errors.Add(Error(row.Row, "athleteCode", $"รหัสนักกีฬา {duplicate.Key} ซ้ำกันในไฟล์"));
                }
            }

            var codes = rows.Select(x => x.Athlete.AthleteCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existingCodes = await _db.Athletes
                .Where(a => codes.Contains(a.AthleteCode))
                .Select(a => a.AthleteCode)
                .ToListAsync();
            var existingSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(x => existingSet.Contains(x.Athlete.AthleteCode)))
            {
                errors.Add(Error(row.Row, "athleteCode", $"รหัสนักกีฬา {row.Athlete.AthleteCode} มีอยู่ในระบบแล้ว"));
            }

            if (errors.Count > 0)
            {
                throw new AthleteImportValidationException(errors.OrderBy(x => x.Row).ToList());
            }

            _db.Athletes.AddRange(rows.Select(x => x.Athlete));
            await _db.SaveChangesAsync();
            return new AthleteImportResultDto { ImportedCount = rows.Count, TotalRows = rows.Count };
        }
        catch (AthleteImportValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import athletes. Controller: AthletesController Service: AthleteService Function: ImportAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<AthleteImportResultDto> ImportUpdatesAsync(Stream fileStream, int actionByUserId)
    {
        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null) throw new AthleteImportValidationException([Error(0, "file", "ไม่พบแผ่นงานในไฟล์")]);
            var errors = ValidateHeaders(sheet);
            if (errors.Count > 0) throw new AthleteImportValidationException(errors);

            var rows = new List<(int Row, Athlete Values)>();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? HeaderRow;
            for (var rowNumber = FirstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var cells = Enumerable.Range(1, ImportHeaders.Length).Select(column => sheet.Cell(rowNumber, column).GetFormattedString().Trim()).ToArray();
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                var values = ParseRow(sheet.Row(rowNumber), rowNumber, actionByUserId, errors);
                if (values is not null) rows.Add((rowNumber, values));
            }
            if (rows.Count == 0 && errors.Count == 0) errors.Add(Error(0, "file", "ไม่พบข้อมูลนักกีฬาสำหรับอัปเดต"));

            foreach (var duplicate in rows.GroupBy(x => x.Values.AthleteCode, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
                foreach (var row in duplicate) errors.Add(Error(row.Row, "athleteCode", $"รหัสนักกีฬา {duplicate.Key} ซ้ำกันในไฟล์"));

            var codes = rows.Select(x => x.Values.AthleteCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var existing = await _db.Athletes.Where(a => codes.Contains(a.AthleteCode)).ToListAsync();
            var existingByCode = existing.ToDictionary(a => a.AthleteCode, StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.Where(row => !existingByCode.ContainsKey(row.Values.AthleteCode)))
                errors.Add(Error(row.Row, "athleteCode", $"ไม่พบรหัสนักกีฬา {row.Values.AthleteCode} ในระบบ"));
            if (errors.Count > 0) throw new AthleteImportValidationException(errors.OrderBy(error => error.Row).ToList());

            foreach (var row in rows)
            {
                var athlete = existingByCode[row.Values.AthleteCode];
                athlete.AthleteType = row.Values.AthleteType;
                athlete.FullName = row.Values.FullName;
                athlete.Nickname = row.Values.Nickname;
                athlete.DateOfBirth = row.Values.DateOfBirth;
                athlete.BirthYear = row.Values.BirthYear;
                athlete.PhoneNumber = row.Values.PhoneNumber;
                athlete.ParentName = row.Values.ParentName;
                athlete.ParentPhoneNumber = row.Values.ParentPhoneNumber;
                athlete.AthleteLevel = row.Values.AthleteLevel;
                athlete.JoinDate = row.Values.JoinDate;
                athlete.Province = row.Values.Province;
                athlete.Remarks = row.Values.Remarks;
                athlete.UpdatedByUserId = actionByUserId;
                athlete.UpdatedDate = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return new AthleteImportResultDto { ImportedCount = rows.Count, TotalRows = rows.Count };
        }
        catch (AthleteImportValidationException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update athletes. Service: AthleteService Function: ImportUpdatesAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    private static List<AthleteImportValidationErrorDto> ValidateHeaders(IXLWorksheet sheet)
    {
        var errors = new List<AthleteImportValidationErrorDto>();
        for (var column = 1; column <= ImportHeaders.Length; column++)
        {
            if (!string.Equals(sheet.Cell(HeaderRow, column).GetString().Trim(), ImportHeaders[column - 1], StringComparison.Ordinal))
            {
                errors.Add(Error(HeaderRow, "file", "รูปแบบหัวตารางไม่ถูกต้อง กรุณาใช้ไฟล์เทมเพลตล่าสุด"));
                break;
            }
        }
        return errors;
    }

    private static Athlete? ParseRow(IXLRow row, int rowNumber, int actionByUserId, List<AthleteImportValidationErrorDto> errors)
    {
        var code = row.Cell(1).GetFormattedString().Trim();
        var typeText = row.Cell(2).GetFormattedString().Trim();
        var fullName = row.Cell(3).GetFormattedString().Trim();
        ValidateRequiredLength(code, rowNumber, "athleteCode", "กรุณากรอกรหัสนักกีฬา", 30, errors);
        ValidateRequiredLength(fullName, rowNumber, "fullName", "กรุณากรอกชื่อ-นามสกุล", 200, errors);

        AthleteType? athleteType = typeText switch
        {
            "นักกีฬาในสังกัด" or "Affiliated" => AthleteType.Affiliated,
            "นักกีฬาทั่วไป" or "General" => AthleteType.General,
            _ => null,
        };
        if (athleteType is null)
        {
            errors.Add(Error(rowNumber, "athleteType", "ประเภทนักกีฬาต้องเป็น นักกีฬาในสังกัด หรือ นักกีฬาทั่วไป"));
        }

        var nickname = Optional(row, 4, 100, rowNumber, "nickname", errors);
        var phone = Optional(row, 7, 30, rowNumber, "phoneNumber", errors);
        var parent = Optional(row, 8, 200, rowNumber, "parentName", errors);
        var parentPhone = Optional(row, 9, 30, rowNumber, "parentPhoneNumber", errors);
        var level = Optional(row, 10, 100, rowNumber, "athleteLevel", errors);
        var province = Optional(row, 12, 100, rowNumber, "province", errors);
        var remarks = row.Cell(13).GetFormattedString().Trim();
        var birthDate = ParseDate(row.Cell(5), rowNumber, "dateOfBirth", errors);
        var birthYear = ParseBirthYear(row.Cell(6), birthDate, rowNumber, errors);
        var joinDate = ParseDate(row.Cell(11), rowNumber, "joinDate", errors);

        if (errors.Any(x => x.Row == rowNumber))
        {
            return null;
        }

        return new Athlete
        {
            AthleteCode = code,
            AthleteType = athleteType!.Value,
            FullName = fullName,
            Nickname = nickname,
            DateOfBirth = birthDate,
            BirthYear = birthYear,
            PhoneNumber = phone,
            ParentName = parent,
            ParentPhoneNumber = parentPhone,
            AthleteLevel = level,
            JoinDate = joinDate,
            Province = province,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks,
            IsActive = true,
            CreatedByUserId = actionByUserId,
        };
    }

    private static string? Optional(IXLRow row, int column, int maxLength, int rowNumber, string field, List<AthleteImportValidationErrorDto> errors)
    {
        var value = row.Cell(column).GetFormattedString().Trim();
        if (value.Length > maxLength)
        {
            errors.Add(Error(rowNumber, field, $"ข้อมูลยาวเกิน {maxLength} ตัวอักษร"));
        }
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void ValidateRequiredLength(string value, int rowNumber, string field, string requiredMessage, int maxLength, List<AthleteImportValidationErrorDto> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(Error(rowNumber, field, requiredMessage));
        else if (value.Length > maxLength) errors.Add(Error(rowNumber, field, $"ข้อมูลยาวเกิน {maxLength} ตัวอักษร"));
    }

    private static DateOnly? ParseDate(IXLCell cell, int rowNumber, string field, List<AthleteImportValidationErrorDto> errors)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var dateTime)) return DateOnly.FromDateTime(dateTime);
        var value = cell.GetFormattedString().Trim();
        var formats = new[] { "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd" };
        if (DateOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        errors.Add(Error(rowNumber, field, "รูปแบบวันที่ไม่ถูกต้อง กรุณาใช้ วว/ดด/ปปปป (ค.ศ.)"));
        return null;
    }

    private static int? ParseBirthYear(IXLCell cell, DateOnly? birthDate, int rowNumber, List<AthleteImportValidationErrorDto> errors)
    {
        if (cell.IsEmpty()) return birthDate?.Year;
        var value = cell.GetFormattedString().Trim();
        if (!int.TryParse(value, out var year) || year < 1900 || year > DateTime.UtcNow.Year)
        {
            errors.Add(Error(rowNumber, "birthYear", $"ปีเกิดต้องอยู่ระหว่าง 1900 ถึง {DateTime.UtcNow.Year}"));
            return null;
        }
        if (birthDate is not null && birthDate.Value.Year != year)
            errors.Add(Error(rowNumber, "birthYear", "ปีเกิดไม่ตรงกับวันเกิด"));
        return year;
    }

    private static AthleteImportValidationErrorDto Error(int row, string field, string message) => new() { Row = row, Field = field, Message = message };

    public async Task<List<AthleteOptionDto>> SearchActiveAsync(string? search)
    {
        var query = _db.Athletes.Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpper();
            query = query.Where(a =>
                a.AthleteCode.ToUpper().Contains(term) ||
                a.FullName.ToUpper().Contains(term) ||
                (a.Nickname != null && a.Nickname.ToUpper().Contains(term)));
        }

        return await query
            .OrderBy(a => a.FullName)
            .Take(SearchResultLimit)
            .Select(a => new AthleteOptionDto
            {
                AthleteId = a.AthleteId,
                AthleteCode = a.AthleteCode,
                FullName = a.FullName,
                Nickname = a.Nickname,
                AthleteType = a.AthleteType,
            })
            .ToListAsync();
    }

    private static AthleteListItemDto MapToListItem(Athlete athlete) => new()
    {
        AthleteId = athlete.AthleteId,
        AthleteCode = athlete.AthleteCode,
        AthleteType = athlete.AthleteType,
        FullName = athlete.FullName,
        Nickname = athlete.Nickname,
        DateOfBirth = athlete.DateOfBirth,
        BirthYear = athlete.BirthYear ?? athlete.DateOfBirth?.Year,
        Age = CalculateAge(athlete),
        AthleteLevel = athlete.AthleteLevel,
        Province = athlete.Province,
        IsActive = athlete.IsActive,
    };

    private static AthleteDetailDto MapToDetail(Athlete athlete) => new()
    {
        AthleteId = athlete.AthleteId,
        AthleteCode = athlete.AthleteCode,
        AthleteType = athlete.AthleteType,
        FullName = athlete.FullName,
        Nickname = athlete.Nickname,
        DateOfBirth = athlete.DateOfBirth,
        BirthYear = athlete.BirthYear ?? athlete.DateOfBirth?.Year,
        Age = CalculateAge(athlete),
        PhoneNumber = athlete.PhoneNumber,
        ParentName = athlete.ParentName,
        ParentPhoneNumber = athlete.ParentPhoneNumber,
        AthleteLevel = athlete.AthleteLevel,
        JoinDate = athlete.JoinDate,
        Province = athlete.Province,
        IsActive = athlete.IsActive,
        Remarks = athlete.Remarks,
    };

    private static int? CalculateAge(Athlete athlete)
    {
        var year = athlete.BirthYear ?? athlete.DateOfBirth?.Year;
        return year is null ? null : DateTime.UtcNow.Year - year.Value;
    }

    private static string? ValidateBirthYear(int? birthYear, DateOnly? birthDate)
    {
        if (birthYear is not null && (birthYear < 1900 || birthYear > DateTime.UtcNow.Year))
            return $"ปีเกิดต้องอยู่ระหว่าง 1900 ถึง {DateTime.UtcNow.Year}";
        if (birthYear is not null && birthDate is not null && birthYear != birthDate.Value.Year)
            return "ปีเกิดไม่ตรงกับวันเกิด";
        return null;
    }
}
