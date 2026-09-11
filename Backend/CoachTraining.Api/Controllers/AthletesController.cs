using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>
/// Athlete Management (requirement.md 4.3, todo.md 4.2). Master-data changes are
/// Administrator only; the active-athlete search is available to any signed-in
/// role since Coaches need it to record Routine/Private attendance.
/// </summary>
[ApiController]
[Route("api/athletes")]
[Authorize]
public class AthletesController : ControllerBase
{
    private const long MaxImportFileSize = 5 * 1024 * 1024;
    private readonly IAthleteService _athleteService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AthletesController> _logger;

    public AthletesController(IAthleteService athleteService, ICurrentUserService currentUser, ILogger<AthletesController> logger)
    {
        _athleteService = athleteService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> List([FromQuery] PagedRequest request, [FromQuery] AthleteType athleteType = AthleteType.Affiliated, [FromQuery] int? age = null)
    {
        try
        {
            if (age is < 0 or > 150)
                return BadRequest(new ApiErrorResponse("อายุต้องอยู่ระหว่าง 0 ถึง 150 ปี"));
            var result = await _athleteService.ListAsync(request, athleteType, age);
            return Ok(new ApiResponse<PagedResponse<AthleteListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes Controller: AthletesController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายชื่อนักกีฬาได้"));
        }
    }

    /// <summary>Used by Routine attendance selection and Private Training athlete assignment.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchActive([FromQuery] string? search)
    {
        var options = await _athleteService.SearchActiveAsync(search);
        return Ok(new ApiResponse<List<AthleteOptionDto>>(options));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetById(int id)
    {
        var athlete = await _athleteService.GetByIdAsync(id);
        if (athlete is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลนักกีฬาที่ต้องการ"));
        }

        return Ok(new ApiResponse<AthleteDetailDto>(athlete));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([FromBody] AthleteCreateDto dto)
    {
        try
        {
            var (result, error) = await _athleteService.CreateAsync(dto, _currentUser.UserId!.Value);
            if (error is not null)
            {
                return BadRequest(new ApiErrorResponse(error));
            }

            return CreatedAtAction(nameof(GetById), new { id = result!.AthleteId }, new ApiResponse<AthleteDetailDto>(result, "สร้างข้อมูลนักกีฬาสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes Controller: AthletesController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างข้อมูลนักกีฬาได้"));
        }
    }

    [HttpGet("import-template")]
    [Authorize(Roles = Roles.Administrator)]
    public IActionResult DownloadImportTemplate()
    {
        try
        {
            var content = _athleteService.CreateImportTemplate();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "athlete-import-template.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes/import-template Controller: AthletesController Function: DownloadImportTemplate UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถดาวน์โหลดเทมเพลตได้"));
        }
    }

    [HttpPost("import")]
    [Authorize(Roles = Roles.Administrator)]
    [RequestSizeLimit(MaxImportFileSize)]
    public async Task<IActionResult> Import([FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("กรุณาเลือกไฟล์สำหรับนำเข้า"));
        }
        if (file.Length > MaxImportFileSize || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiErrorResponse("รองรับเฉพาะไฟล์ .xlsx ขนาดไม่เกิน 5 MB"));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _athleteService.ImportAsync(stream, _currentUser.UserId!.Value);
            return Ok(new ApiResponse<AthleteImportResultDto>(result, $"นำเข้านักกีฬา {result.ImportedCount} รายการสำเร็จ"));
        }
        catch (AthleteImportValidationException ex)
        {
            return BadRequest(new { message = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes/import Controller: AthletesController Function: Import UserId: {UserId} FileName: {FileName}", _currentUser.UserId, Path.GetFileName(file.FileName));
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถนำเข้ารายชื่อนักกีฬาได้"));
        }
    }

    [HttpGet("update-template")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> DownloadUpdateTemplate()
    {
        try
        {
            var content = await _athleteService.CreateUpdateTemplateAsync();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "athlete-bulk-update.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes/update-template Controller: AthletesController Function: DownloadUpdateTemplate UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถดาวน์โหลดแบบฟอร์มอัปเดตได้"));
        }
    }

    [HttpPost("import-update")]
    [Authorize(Roles = Roles.Administrator)]
    [RequestSizeLimit(MaxImportFileSize)]
    public async Task<IActionResult> ImportUpdates([FromForm] IFormFile? file)
    {
        if (file is null || file.Length == 0) return BadRequest(new ApiErrorResponse("กรุณาเลือกไฟล์สำหรับอัปเดต"));
        if (file.Length > MaxImportFileSize || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ApiErrorResponse("รองรับเฉพาะไฟล์ .xlsx ขนาดไม่เกิน 5 MB"));
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _athleteService.ImportUpdatesAsync(stream, _currentUser.UserId!.Value);
            return Ok(new ApiResponse<AthleteImportResultDto>(result, $"อัปเดตนักกีฬา {result.ImportedCount} รายการสำเร็จ"));
        }
        catch (AthleteImportValidationException ex) { return BadRequest(new { message = ex.Message, errors = ex.Errors }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/athletes/import-update Controller: AthletesController Function: ImportUpdates UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถอัปเดตข้อมูลนักกีฬาได้"));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Update(int id, [FromBody] AthleteUpdateDto dto)
    {
        var (result, error) = await _athleteService.UpdateAsync(id, dto, _currentUser.UserId!.Value);
        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (result is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลนักกีฬาที่ต้องการ"));
        }

        return Ok(new ApiResponse<AthleteDetailDto>(result, "บันทึกข้อมูลสำเร็จ"));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> SetStatus(int id, [FromBody] AthleteStatusUpdateDto dto)
    {
        var success = await _athleteService.SetStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลนักกีฬาที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, dto.IsActive ? "เปิดใช้งานนักกีฬาสำเร็จ" : "ปิดใช้งานนักกีฬาสำเร็จ"));
    }
}
