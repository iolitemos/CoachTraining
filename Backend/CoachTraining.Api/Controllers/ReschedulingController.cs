using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Reschedules;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Administrator-only session rescheduling (FR-CR-004–007).</summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize(Roles = Roles.Administrator)]
public class ReschedulingController : ControllerBase
{
    private readonly IReschedulingService _reschedulingService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReschedulingController> _logger;

    public ReschedulingController(IReschedulingService reschedulingService, ICurrentUserService currentUser, ILogger<ReschedulingController> logger)
    {
        _reschedulingService = reschedulingService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleSessionRequest request)
    {
        try
        {
            var result = await _reschedulingService.RescheduleAsync(id, request, _currentUser.UserId!.Value);

            if (result.NotFound)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
            }

            if (result.Conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    result.Error!,
                    result.Conflicts
                        .Select(c => new ApiFieldError(c.AthleteId is not null ? "athleteIds" : "coachId", c.Message))
                        .ToList()));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<RescheduleResponseDto>(
                new RescheduleResponseDto { OriginalSession = result.OriginalSession!, ReplacementSession = result.ReplacementSession! },
                "เลื่อนเซสชันฝึกซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Route: api/training-sessions/{Id}/reschedule Controller: ReschedulingController Function: Reschedule UserId: {UserId}",
                id,
                _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเลื่อนเซสชันฝึกซ้อมได้"));
        }
    }
}
