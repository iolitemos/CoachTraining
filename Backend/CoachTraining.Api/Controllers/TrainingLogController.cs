using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingLogs;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Training Log (requirement.md 4.9, todo.md 4.10) — Coach or Administrator.</summary>
[ApiController]
[Route("api/training-sessions/{sessionId:int}/training-log")]
[Authorize(Roles = $"{Roles.Coach},{Roles.Administrator}")]
public class TrainingLogController : ControllerBase
{
    private readonly ITrainingLogService _trainingLogService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TrainingLogController> _logger;

    public TrainingLogController(ITrainingLogService trainingLogService, ICurrentUserService currentUser, ILogger<TrainingLogController> logger)
    {
        _trainingLogService = trainingLogService;
        _currentUser = currentUser;
        _logger = logger;
    }

    private bool IsAdministrator => _currentUser.IsInRole(Roles.Administrator);

    [HttpGet]
    public async Task<IActionResult> Get(int sessionId)
    {
        try
        {
            var result = await _trainingLogService.GetAsync(sessionId, IsAdministrator, _currentUser.CoachId);
            if (result is null)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
            }

            return Ok(new ApiResponse<TrainingLogDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/training-log Controller: TrainingLogController Function: Get UserId: {UserId}", sessionId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดบันทึกการฝึกซ้อมได้"));
        }
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(int sessionId, [FromBody] TrainingLogUpsertRequest request)
    {
        try
        {
            var result = await _trainingLogService.UpsertAsync(sessionId, request, IsAdministrator, _currentUser.CoachId, _currentUser.UserId!.Value);

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

            return Ok(new ApiResponse<TrainingLogDto>(result.TrainingLog!, "บันทึกข้อมูลสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/training-log Controller: TrainingLogController Function: Upsert UserId: {UserId}", sessionId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถบันทึกข้อมูลได้"));
        }
    }
}
