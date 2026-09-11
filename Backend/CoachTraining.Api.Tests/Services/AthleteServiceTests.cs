using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AthleteServiceTests
{
    private static AthleteService CreateService(Data.ApplicationDbContext db) =>
        new(db, NullLogger<AthleteService>.Instance);

    [Fact]
    public async Task CreateAsync_WithDuplicateAthleteCode_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete A" }, actionByUserId: 1);
        var (result, error) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete B" }, actionByUserId: 1);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SetStatusAsync_Deactivate_DoesNotDeleteTheAthleteRecord()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (created, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete A" }, actionByUserId: 1);

        var success = await service.SetStatusAsync(created!.AthleteId, isActive: false, actionByUserId: 1);
        var detail = await service.GetByIdAsync(created.AthleteId);

        Assert.True(success);
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);
    }

    [Fact]
    public async Task SearchActiveAsync_ExcludesInactiveAthletes()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (active, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Active Athlete" }, actionByUserId: 1);
        var (inactive, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", FullName = "Inactive Athlete" }, actionByUserId: 1);
        await service.SetStatusAsync(inactive!.AthleteId, isActive: false, actionByUserId: 1);

        var results = await service.SearchActiveAsync(null);

        Assert.Contains(results, a => a.AthleteId == active!.AthleteId);
        Assert.DoesNotContain(results, a => a.AthleteId == inactive.AthleteId);
    }

    [Fact]
    public async Task SearchActiveAsync_FiltersByNameOrCode()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Somchai Jaidee" }, actionByUserId: 1);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", FullName = "Somsri Rakdee" }, actionByUserId: 1);

        var results = await service.SearchActiveAsync("Somchai");

        Assert.Single(results);
        Assert.Equal("A001", results[0].AthleteCode);
    }

    [Fact]
    public async Task CreateAndUpdateAsync_PersistsAthleteType()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var (created, createError) = await service.CreateAsync(new AthleteCreateDto
        {
            AthleteCode = "A001",
            AthleteType = AthleteType.General,
            FullName = "General Athlete",
        }, actionByUserId: 1);

        Assert.Null(createError);
        Assert.Equal(AthleteType.General, created!.AthleteType);

        var (updated, updateError) = await service.UpdateAsync(created.AthleteId, new AthleteUpdateDto
        {
            AthleteType = AthleteType.Affiliated,
            FullName = created.FullName,
        }, actionByUserId: 1);

        Assert.Null(updateError);
        Assert.Equal(AthleteType.Affiliated, updated!.AthleteType);
    }

    [Fact]
    public async Task ListAsync_FiltersAthletesByTypeBeforePagination()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", AthleteType = AthleteType.Affiliated, FullName = "Affiliated Athlete" }, 1);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", AthleteType = AthleteType.General, FullName = "General Athlete" }, 1);

        var result = await service.ListAsync(new DTOs.Common.PagedRequest { Page = 1, PageSize = 20 }, AthleteType.General);

        Assert.Single(result.Items);
        Assert.Equal("A002", result.Items[0].AthleteCode);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_FiltersByExactAgeAndReturnsBirthYearAndAge()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var targetAge = 14;
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Matched", BirthYear = DateTime.UtcNow.Year - targetAge }, 1);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", FullName = "Other", BirthYear = DateTime.UtcNow.Year - 20 }, 1);

        var result = await service.ListAsync(new DTOs.Common.PagedRequest { Page = 1, PageSize = 20 }, AthleteType.Affiliated, targetAge);

        Assert.Single(result.Items);
        Assert.Equal(DateTime.UtcNow.Year - targetAge, result.Items[0].BirthYear);
        Assert.Equal(targetAge, result.Items[0].Age);
    }

    [Fact]
    public void CreateImportTemplate_ContainsExpectedHeadersAndValidation()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var bytes = service.CreateImportTemplate();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("รายชื่อนักกีฬา");

        Assert.Equal("รหัสนักกีฬา*", sheet.Cell("A3").GetString());
        Assert.Equal("ประเภทนักกีฬา*", sheet.Cell("B3").GetString());
        Assert.Equal("ชื่อ-นามสกุล*", sheet.Cell("C3").GetString());
        Assert.Equal("ปีเกิด", sheet.Cell("F3").GetString());
        Assert.Equal("จังหวัด", sheet.Cell("L3").GetString());
        Assert.NotEmpty(sheet.DataValidations);
    }

    [Fact]
    public async Task ImportAsync_WithValidRows_CreatesAllAthletes()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        using var stream = CreateImportFile(
            ["A001", "นักกีฬาในสังกัด", "สมชาย ใจดี", "ชาย", new DateTime(2012, 5, 15), 2012, "0812345678", "", "", "เยาวชน", new DateTime(2026, 1, 10), "เชียงใหม่", ""],
            ["A002", "General", "สมหญิง รักดี", "หญิง", "", 2014, "", "", "", "", "", "กรุงเทพมหานคร", ""]);

        var result = await service.ImportAsync(stream, 1);

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, await db.Athletes.CountAsync());
        Assert.Contains(db.Athletes, athlete => athlete.AthleteCode == "A002" && athlete.AthleteType == AthleteType.General);
        Assert.Contains(db.Athletes, athlete => athlete.AthleteCode == "A001" && athlete.Province == "เชียงใหม่");
    }

    [Fact]
    public async Task ImportAsync_WithDuplicateOrInvalidRows_DoesNotPersistAnyRows()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Existing" }, 1);
        using var stream = CreateImportFile(
            ["A001", "นักกีฬาในสังกัด", "Duplicate", "", "", "", "", "", "", "", "", "", ""],
            ["A002", "ประเภทไม่ถูกต้อง", "Invalid", "", "", "", "", "", "", "", "", "", ""]);

        var exception = await Assert.ThrowsAsync<AthleteImportValidationException>(() => service.ImportAsync(stream, 1));

        Assert.Contains(exception.Errors, error => error.Row == 4 && error.Field == "athleteCode");
        Assert.Contains(exception.Errors, error => error.Row == 5 && error.Field == "athleteType");
        Assert.Equal(1, await db.Athletes.CountAsync());
    }

    [Fact]
    public async Task UpdateTemplateAndImportUpdatesAsync_UpdatesExistingAthletesByCode()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await service.CreateAsync(new AthleteCreateDto
        {
            AthleteCode = "A001",
            FullName = "Original Name",
            BirthYear = 2012,
        }, 1);

        var template = await service.CreateUpdateTemplateAsync();
        using var workbook = new XLWorkbook(new MemoryStream(template));
        var sheet = workbook.Worksheet("รายชื่อนักกีฬา");
        Assert.Equal("A001", sheet.Cell("A4").GetString());
        sheet.Cell("C4").Value = "Updated Name";
        sheet.Cell("F4").Value = 2013;
        sheet.Cell("L4").Value = "ภูเก็ต";
        using var updatedFile = new MemoryStream();
        workbook.SaveAs(updatedFile);
        updatedFile.Position = 0;

        var result = await service.ImportUpdatesAsync(updatedFile, 2);
        var athlete = await db.Athletes.SingleAsync();

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("Updated Name", athlete.FullName);
        Assert.Equal(2013, athlete.BirthYear);
        Assert.Equal("ภูเก็ต", athlete.Province);
        Assert.Equal(2, athlete.UpdatedByUserId);
    }

    private static MemoryStream CreateImportFile(params object[][] rows)
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("รายชื่อนักกีฬา");
        var headers = new[]
        {
            "รหัสนักกีฬา*", "ประเภทนักกีฬา*", "ชื่อ-นามสกุล*", "ชื่อเล่น", "วันเกิด", "ปีเกิด",
            "เบอร์ติดต่อ", "ชื่อผู้ปกครอง", "เบอร์ติดต่อผู้ปกครอง", "ระดับนักกีฬา", "วันที่เข้าร่วม", "จังหวัด", "หมายเหตุ"
        };
        for (var column = 1; column <= headers.Length; column++) sheet.Cell(3, column).Value = headers[column - 1];
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < rows[row].Length; column++)
            {
                sheet.Cell(row + 4, column + 1).Value = XLCellValue.FromObject(rows[row][column]);
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        workbook.Dispose();
        stream.Position = 0;
        return stream;
    }
}
