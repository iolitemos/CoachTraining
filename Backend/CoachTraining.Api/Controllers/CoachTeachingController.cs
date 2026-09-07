using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.CoachTeaching;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Coach Teaching Record actions (requirement.md 4.7, todo.md 4.7) — Coach or Administrator.</summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize(Roles = $"{Roles.Coach},{Roles.Administrator}")]
public class CoachTeachingController : ControllerBase
{
    private readonly ICoachTeachingService _coachTeachingService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachTeachingController> _logger;

    public CoachTeachingController(ICoachTeachingService coachTeachingService, ICurrentUserService currentUser, ILogger<CoachTeachingController> logger)
    {
        _coachTeachingService = coachTeachingService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id, [FromBody] TeachingStartRequest request)
    {
        try
        {
            var result = await _coachTeachingService.StartAsync(id, request, _currentUser.IsInRole(Roles.Administrator), _currentUser.CoachId, _currentUser.UserId!.Value);
            return ToActionResult(result, "เริ่มฝึกซ้อมสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/start Controller: CoachTeachingController Function: Start UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเริ่มฝึกซ้อมได้"));
        }
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] TeachingEndRequest request)
    {
        try
        {
            var result = await _coachTeachingService.CompleteAsync(id, request, _currentUser.IsInRole(Roles.Administrator), _currentUser.CoachId, _currentUser.UserId!.Value);
            return ToActionResult(result, "บันทึกการเสร็จสิ้นฝึกซ้อมสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/complete Controller: CoachTeachingController Function: Complete UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถบันทึกการเสร็จสิ้นฝึกซ้อมได้"));
        }
    }

    private IActionResult ToActionResult(TeachingActionResult result, string successMessage)
    {
        if (result.NotFound)
        {
            return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
        }

        if (result.Forbidden)
        {
            return Forbid();
        }

        if (result.Error is not null)
        {
            return BadRequest(new ApiErrorResponse(result.Error));
        }

        return Ok(new ApiResponse<TrainingSessionDetailDto>(result.Session!, successMessage));
    }
}
