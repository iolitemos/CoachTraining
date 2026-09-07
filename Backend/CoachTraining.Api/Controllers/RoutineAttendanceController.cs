using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineAttendance;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>
/// Routine Attendance (requirement.md 4.8, todo.md 4.8) — Coach or Administrator.
/// No pre-assigned roster: only athletes the Coach actively adds get a record.
/// </summary>
[ApiController]
[Route("api/training-sessions/{sessionId:int}/routine-attendance")]
[Authorize(Roles = $"{Roles.Coach},{Roles.Administrator}")]
public class RoutineAttendanceController : ControllerBase
{
    private readonly IRoutineAttendanceService _routineAttendanceService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RoutineAttendanceController> _logger;

    public RoutineAttendanceController(IRoutineAttendanceService routineAttendanceService, ICurrentUserService currentUser, ILogger<RoutineAttendanceController> logger)
    {
        _routineAttendanceService = routineAttendanceService;
        _currentUser = currentUser;
        _logger = logger;
    }

    private bool IsAdministrator => _currentUser.IsInRole(Roles.Administrator);

    [HttpGet]
    public async Task<IActionResult> List(int sessionId)
    {
        try
        {
            var result = await _routineAttendanceService.ListAsync(sessionId, IsAdministrator, _currentUser.CoachId);
            if (result is null)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมประจำที่ต้องการ"));
            }

            return Ok(new ApiResponse<List<RoutineAttendanceListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/routine-attendance Controller: RoutineAttendanceController Function: List UserId: {UserId}", sessionId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดข้อมูลการเข้าร่วมได้"));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add(int sessionId, [FromBody] RoutineAttendanceCreateDto dto)
    {
        try
        {
            var result = await _routineAttendanceService.AddAsync(sessionId, dto, IsAdministrator, _currentUser.CoachId, _currentUser.UserId!.Value);
            return ToActionResult(result, "เพิ่มการเข้าร่วมสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/routine-attendance Controller: RoutineAttendanceController Function: Add UserId: {UserId}", sessionId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเพิ่มการเข้าร่วมได้"));
        }
    }

    [HttpPut("{attendanceId:int}")]
    public async Task<IActionResult> Update(int sessionId, int attendanceId, [FromBody] RoutineAttendanceUpdateDto dto)
    {
        var result = await _routineAttendanceService.UpdateAsync(sessionId, attendanceId, dto, IsAdministrator, _currentUser.CoachId, _currentUser.UserId!.Value);
        return ToActionResult(result, "บันทึกข้อมูลสำเร็จ");
    }

    [HttpDelete("{attendanceId:int}")]
    public async Task<IActionResult> Remove(int sessionId, int attendanceId)
    {
        var result = await _routineAttendanceService.RemoveAsync(sessionId, attendanceId, IsAdministrator, _currentUser.CoachId);
        return ToActionResult(result, "ลบการเข้าร่วมสำเร็จ");
    }

    private IActionResult ToActionResult(RoutineAttendanceActionResult result, string successMessage)
    {
        if (result.NotFound)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลที่ต้องการ"));
        }

        if (result.Forbidden)
        {
            return Forbid();
        }

        if (result.Error is not null)
        {
            return BadRequest(new ApiErrorResponse(result.Error));
        }

        return result.Attendance is not null
            ? Ok(new ApiResponse<RoutineAttendanceListItemDto>(result.Attendance, successMessage))
            : Ok(new ApiResponse<object>(new { }, successMessage));
    }
}
