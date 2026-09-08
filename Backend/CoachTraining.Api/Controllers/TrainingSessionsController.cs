using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>
/// Unified Training Session query API (requirement.md 4.6, todo.md 4.5) — every
/// signed-in role may call this; a Coach account only ever sees its own sessions
/// (CLAUDE.md section 11), enforced in the service layer.
/// </summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize]
public class TrainingSessionsController : ControllerBase
{
    private readonly ITrainingSessionService _trainingSessionService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TrainingSessionsController> _logger;

    public TrainingSessionsController(ITrainingSessionService trainingSessionService, ICurrentUserService currentUser, ILogger<TrainingSessionsController> logger)
    {
        _trainingSessionService = trainingSessionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    private bool IsPrivilegedRole => _currentUser.IsInRole(Roles.Administrator) || _currentUser.IsInRole(Roles.ManagementViewer);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TrainingSessionFilterRequest filter)
    {
        try
        {
            var result = await _trainingSessionService.ListAsync(filter, IsPrivilegedRole, _currentUser.CoachId);
            return Ok(new ApiResponse<PagedResponse<TrainingSessionListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions Controller: TrainingSessionsController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายการฝึกซ้อมได้"));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var session = await _trainingSessionService.GetByIdAsync(id, IsPrivilegedRole, _currentUser.CoachId);
        if (session is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
        }

        return Ok(new ApiResponse<TrainingSessionDetailDto>(session));
    }

    [HttpPost("{id:int}/reset-to-scheduled")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> ResetToScheduled(int id, [FromBody] ResetSessionRequest request)
    {
        var result = await _trainingSessionService.ResetToScheduledAsync(id, request.Reason, _currentUser.UserId!.Value);
        if (result.NotFound) return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
        if (result.Error is not null) return BadRequest(new ApiErrorResponse(result.Error));
        return Ok(new ApiResponse<TrainingSessionDetailDto>(result.Session!, "ดึงสถานะกลับเป็นกำหนดการสำเร็จ"));
    }
}
