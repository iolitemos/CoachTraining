using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Cancellations;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Administrator-only session cancellation (FR-CR-001–003).</summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize(Roles = Roles.Administrator)]
public class CancellationsController : ControllerBase
{
    private readonly ICancellationService _cancellationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CancellationsController> _logger;

    public CancellationsController(
        ICancellationService cancellationService,
        ICurrentUserService currentUser,
        ILogger<CancellationsController> logger)
    {
        _cancellationService = cancellationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelSessionRequest request)
    {
        try
        {
            var result = await _cancellationService.CancelAsync(id, request, _currentUser.UserId!.Value);

            if (result.NotFound)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<TrainingSessionDetailDto>(result.Session!, "ยกเลิกเซสชันฝึกซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Route: api/training-sessions/{Id}/cancel Controller: CancellationsController Function: Cancel UserId: {UserId}",
                id,
                _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถยกเลิกเซสชันฝึกซ้อมได้"));
        }
    }
}
