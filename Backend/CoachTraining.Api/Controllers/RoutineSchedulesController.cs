using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Routine Training Management (requirement.md 4.4, todo.md 4.3) — Administrator only.</summary>
[ApiController]
[Route("api/routine-schedules")]
[Authorize(Roles = Roles.Administrator)]
public class RoutineSchedulesController : ControllerBase
{
    private readonly IRoutineScheduleService _routineScheduleService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RoutineSchedulesController> _logger;

    public RoutineSchedulesController(IRoutineScheduleService routineScheduleService, ICurrentUserService currentUser, ILogger<RoutineSchedulesController> logger)
    {
        _routineScheduleService = routineScheduleService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        try
        {
            var result = await _routineScheduleService.ListAsync(request);
            return Ok(new ApiResponse<PagedResponse<RoutineScheduleListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/routine-schedules Controller: RoutineSchedulesController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดตารางฝึกซ้อมได้"));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var schedule = await _routineScheduleService.GetByIdAsync(id);
        if (schedule is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
        }

        return Ok(new ApiResponse<RoutineScheduleDetailDto>(schedule));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoutineScheduleCreateDto dto)
    {
        try
        {
            var result = await _routineScheduleService.CreateAsync(dto, _currentUser.UserId!.Value);

            if (result.Conflicts.Count > 0)
            {
                return Conflict(ToConflictResponse(result.Error!, result.Conflicts.Select(c => c.Message)));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Schedule!.RoutineScheduleId },
                new ApiResponse<RoutineScheduleSaveResult>(result, "สร้างตารางฝึกซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/routine-schedules Controller: RoutineSchedulesController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างตารางฝึกซ้อมได้"));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoutineScheduleUpdateDto dto)
    {
        var result = await _routineScheduleService.UpdateAsync(id, dto, _currentUser.UserId!.Value);

        if (result.Conflicts.Count > 0)
        {
            return Conflict(ToConflictResponse(result.Error!, result.Conflicts.Select(c => c.Message)));
        }

        if (result.Error is not null)
        {
            return BadRequest(new ApiErrorResponse(result.Error));
        }

        if (result.Schedule is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
        }

        return Ok(new ApiResponse<RoutineScheduleDetailDto>(result.Schedule, "บันทึกข้อมูลสำเร็จ"));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] RoutineScheduleStatusUpdateDto dto)
    {
        var success = await _routineScheduleService.SetStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, dto.IsActive ? "เปิดใช้งานตารางฝึกซ้อมสำเร็จ" : "ปิดใช้งานตารางฝึกซ้อมสำเร็จ"));
    }

    /// <summary>Extends generated sessions for this schedule further into the future.</summary>
    [HttpPost("{id:int}/generate-sessions")]
    public async Task<IActionResult> GenerateSessions(int id, [FromBody] GenerateSessionsRequest request)
    {
        var (result, error) = await _routineScheduleService.GenerateSessionsAsync(id, request.ThroughDate, _currentUser.UserId!.Value);

        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (result is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
        }

        return Ok(new ApiResponse<GenerateSessionsResult>(result, "สร้างรอบฝึกซ้อมสำเร็จ"));
    }

    private static ApiErrorResponse ToConflictResponse(string message, IEnumerable<string> conflictMessages) =>
        new(message, conflictMessages.Select(m => new ApiFieldError("coachId", m)).ToList());
}
