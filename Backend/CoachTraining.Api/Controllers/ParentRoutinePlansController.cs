using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.ParentRoutinePlans;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/parent-routine-plans")]
public class ParentRoutinePlansController : ControllerBase
{
    private readonly IParentRoutinePlanService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ParentRoutinePlansController> _logger;

    public ParentRoutinePlansController(IParentRoutinePlanService service, ICurrentUserService currentUser, ILogger<ParentRoutinePlansController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("admin/athletes/{athleteId:int}/link")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetLinkStatus(int athleteId, CancellationToken cancellationToken)
    {
        var result = await _service.GetLinkStatusAsync(athleteId, cancellationToken);
        return result is null ? NotFound(new ApiErrorResponse("ไม่พบข้อมูลนักกีฬา")) : Ok(new ApiResponse<ParentRoutinePlanLinkStatusDto>(result));
    }

    [HttpPost("admin/athletes/{athleteId:int}/link")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> RotateLink(int athleteId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.RotateLinkAsync(athleteId, _currentUser.UserId!.Value, cancellationToken);
            return result is null
                ? BadRequest(new ApiErrorResponse("ไม่พบนักกีฬาที่เปิดใช้งาน"))
                : Ok(new ApiResponse<ParentRoutinePlanLinkCreatedDto>(result, "สร้างลิงก์ลงแผนเข้าซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/parent-routine-plans/admin/athletes/{AthleteId}/link Controller: ParentRoutinePlansController Function: RotateLink UserId: {UserId}", athleteId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างลิงก์ได้"));
        }
    }

    [HttpPatch("admin/athletes/{athleteId:int}/link/access")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> SetLinkAccess(int athleteId, [FromBody] ParentRoutinePlanAccessRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.SetLinkAccessAsync(athleteId, request.IsEnabled, _currentUser.UserId!.Value, cancellationToken);
        return result is null
            ? NotFound(new ApiErrorResponse("ยังไม่มีลิงก์ลงแผนเข้าซ้อมของนักกีฬาคนนี้"))
            : Ok(new ApiResponse<ParentRoutinePlanLinkStatusDto>(result, request.IsEnabled ? "เปิดการเข้าถึงแล้ว" : "ปิดการเข้าถึงชั่วคราวแล้ว"));
    }

    [HttpGet("admin/summary")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetSummary([FromQuery] ParentRoutinePlanRangeRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetRange(request, out var startDate, out var endDate, out var error)) return BadRequest(error);
        try { return Ok(new ApiResponse<IReadOnlyList<RoutineParticipationPlanSummaryDto>>(await _service.GetSummaryAsync(startDate, endDate, cancellationToken))); }
        catch (ArgumentException ex) { return BadRequest(new ApiErrorResponse(ex.Message)); }
    }

    [HttpGet("admin/dates/{trainingDate}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetAthletes(DateOnly trainingDate, CancellationToken cancellationToken)
    {
        var result = await _service.GetAthletesAsync(trainingDate, cancellationToken);
        return result is null ? NotFound(new ApiErrorResponse("วันที่เลือกไม่ได้ถูกกำหนดเป็นวันฝึกซ้อมประจำ")) : Ok(new ApiResponse<IReadOnlyList<RoutineParticipationPlanAthleteDto>>(result));
    }

    [HttpGet("public/{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicCalendar")]
    public async Task<IActionResult> GetCalendar(string token, [FromQuery] ParentRoutinePlanRangeRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetRange(request, out var startDate, out var endDate, out var error)) return BadRequest(error);
        var result = await _service.GetCalendarAsync(token, startDate, endDate, cancellationToken);
        return result is null ? NotFound(new ApiErrorResponse("ลิงก์ไม่ถูกต้อง ถูกปิด หรือไม่สามารถใช้งานได้")) : Ok(new ApiResponse<ParentRoutinePlanCalendarDto>(result));
    }

    [HttpPut("public/{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicCalendar")]
    public async Task<IActionResult> Save(string token, [FromBody] ParentRoutinePlanSaveRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetRange(new(request.StartDate, request.EndDate), out var startDate, out var endDate, out var error)) return BadRequest(error);
        try
        {
            var result = await _service.SaveAsync(token, startDate, endDate, request.SelectedDates, cancellationToken);
            return result is null ? NotFound(new ApiErrorResponse("ลิงก์ไม่ถูกต้อง ถูกปิด หรือไม่สามารถใช้งานได้")) : Ok(new ApiResponse<ParentRoutinePlanCalendarDto>(result, "บันทึกแผนเข้าซ้อมแล้ว"));
        }
        catch (ArgumentException ex) { return BadRequest(new ApiErrorResponse(ex.Message)); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/parent-routine-plans/public/{TokenHint} Controller: ParentRoutinePlansController Function: Save", token.Length > 6 ? token[^6..] : "invalid");
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถบันทึกแผนเข้าซ้อมได้"));
        }
    }

    private static bool TryGetRange(ParentRoutinePlanRangeRequest request, out DateOnly startDate, out DateOnly endDate, out ApiErrorResponse error)
    {
        startDate = request.StartDate ?? default;
        endDate = request.EndDate ?? default;
        error = new ApiErrorResponse("ช่วงวันที่ต้องถูกต้องและไม่เกิน 63 วัน");
        return request.StartDate is not null && request.EndDate is not null && endDate >= startDate && endDate.DayNumber - startDate.DayNumber <= 62;
    }
}
