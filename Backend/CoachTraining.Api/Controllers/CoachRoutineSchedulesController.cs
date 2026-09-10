using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Restricted Coach self-service creation for the signed-in Coach's own
/// Routine Training schedules.</summary>
[ApiController]
[Route("api/coach/routine-schedules")]
[Authorize(Roles = Roles.Coach)]
public class CoachRoutineSchedulesController : ControllerBase
{
    private readonly IRoutineScheduleService _routineScheduleService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachRoutineSchedulesController> _logger;

    public CoachRoutineSchedulesController(
        IRoutineScheduleService routineScheduleService,
        ICurrentUserService currentUser,
        ILogger<CoachRoutineSchedulesController> logger)
    {
        _routineScheduleService = routineScheduleService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CoachRoutineScheduleCreateDto dto)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var result = await _routineScheduleService.CreateAsync(new RoutineScheduleCreateDto
            {
                CoachId = coachId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                EffectiveStartDate = dto.EffectiveStartDate,
                Remarks = dto.Remarks,
                OverrideConflict = false,
                OverrideReason = null,
            }, userId);

            if (result.Conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    result.Error!,
                    result.Conflicts.Select(c => new ApiFieldError("schedule", c.Message)).ToList()));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return StatusCode(201, new ApiResponse<RoutineScheduleSaveResult>(result, "เพิ่มตารางฝึกซ้อมของคุณสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules Controller: CoachRoutineSchedulesController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเพิ่มตารางฝึกซ้อมได้"));
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch([FromBody] CoachRoutineScheduleBatchCreateDto dto)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var (result, error, conflicts) = await _routineScheduleService.CreateOwnBatchAsync(coachId, dto, userId);
            if (conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    error!, conflicts.Select(message => new ApiFieldError("schedule", message)).ToList()));
            }

            if (error is not null)
            {
                return BadRequest(new ApiErrorResponse(error));
            }

            return StatusCode(201, new ApiResponse<CoachRoutineScheduleBatchCreateResult>(
                result!, $"เพิ่มตารางฝึกซ้อมสำเร็จ {result!.CreatedCount} รายการ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules/batch Controller: CoachRoutineSchedulesController Function: CreateBatch UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเพิ่มตารางฝึกซ้อมได้"));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var result = await _routineScheduleService.DeleteOwnAsync(id, coachId, userId);
            if (!result.Found)
            {
                return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
            }

            if (result.Forbidden)
            {
                return Forbid();
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<object>(new { }, "ลบตารางฝึกซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules/{Id} Controller: CoachRoutineSchedulesController Function: Delete UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถลบตารางฝึกซ้อมได้"));
        }
    }
}
