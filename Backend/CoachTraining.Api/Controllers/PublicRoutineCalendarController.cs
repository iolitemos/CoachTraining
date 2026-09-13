using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.PublicCalendar;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/public/routine-calendar")]
public class PublicRoutineCalendarController : ControllerBase
{
    private readonly IPublicRoutineCalendarService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PublicRoutineCalendarController> _logger;

    public PublicRoutineCalendarController(IPublicRoutineCalendarService service, ICurrentUserService currentUser, ILogger<PublicRoutineCalendarController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("share-link")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetShareLinkStatus(CancellationToken cancellationToken)
    {
        try { return Ok(new ApiResponse<RoutineCalendarShareStatusDto>(await _service.GetStatusAsync(cancellationToken))); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/public/routine-calendar/share-link Controller: PublicRoutineCalendarController Function: GetShareLinkStatus UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดสถานะลิงก์ได้"));
        }
    }

    [HttpPost("share-link")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> RotateShareLink(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.RotateLinkAsync(_currentUser.UserId!.Value, cancellationToken);
            return Ok(new ApiResponse<RoutineCalendarShareCreatedDto>(result, "สร้างลิงก์แชร์สำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/public/routine-calendar/share-link Controller: PublicRoutineCalendarController Function: RotateShareLink UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างลิงก์ได้"));
        }
    }

    [HttpDelete("share-link")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> RevokeShareLink(CancellationToken cancellationToken)
    {
        try
        {
            var revoked = await _service.RevokeLinkAsync(_currentUser.UserId!.Value, cancellationToken);
            return revoked ? NoContent() : NotFound(new ApiErrorResponse("ไม่มีลิงก์ที่กำลังใช้งาน"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/public/routine-calendar/share-link Controller: PublicRoutineCalendarController Function: RevokeShareLink UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถยกเลิกลิงก์ได้"));
        }
    }

    [HttpGet("{token}")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicCalendar")]
    public async Task<IActionResult> GetCalendar(string token, [FromQuery] PublicRoutineCalendarRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate is null || request.EndDate is null
            || request.EndDate < request.StartDate
            || request.EndDate.Value.DayNumber - request.StartDate.Value.DayNumber > 62)
            return BadRequest(new ApiErrorResponse("ช่วงวันที่ต้องถูกต้องและไม่เกิน 63 วัน"));

        var result = await _service.GetCalendarAsync(token, request.StartDate.Value, request.EndDate.Value, cancellationToken);
        return result is null
            ? NotFound(new ApiErrorResponse("ลิงก์ปฏิทินไม่ถูกต้องหรือถูกยกเลิกแล้ว"))
            : Ok(new ApiResponse<PublicRoutineCalendarDto>(result));
    }
}
