using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.PrivateAttendance;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>
/// Private Attendance (requirement.md 4.8, todo.md 4.9) — Coach or Administrator.
/// The roster is fixed by the session's assigned athletes; attendance is recorded
/// per athlete, never added/removed independently of that assignment.
/// </summary>
[ApiController]
[Route("api/training-sessions/{sessionId:int}/private-attendance")]
[Authorize(Roles = $"{Roles.Coach},{Roles.Administrator}")]
public class PrivateAttendanceController : ControllerBase
{
    private readonly IPrivateAttendanceService _privateAttendanceService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PrivateAttendanceController> _logger;

    public PrivateAttendanceController(IPrivateAttendanceService privateAttendanceService, ICurrentUserService currentUser, ILogger<PrivateAttendanceController> logger)
    {
        _privateAttendanceService = privateAttendanceService;
        _currentUser = currentUser;
        _logger = logger;
    }

    private bool IsAdministrator => _currentUser.IsInRole(Roles.Administrator);

    [HttpGet]
    public async Task<IActionResult> GetRoster(int sessionId)
    {
        try
        {
            var result = await _privateAttendanceService.GetRosterAsync(sessionId, IsAdministrator, _currentUser.CoachId);
            if (result is null)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมส่วนตัวที่ต้องการ"));
            }

            return Ok(new ApiResponse<PrivateAttendanceRosterResult>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/private-attendance Controller: PrivateAttendanceController Function: GetRoster UserId: {UserId}", sessionId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดข้อมูลการเข้าร่วมได้"));
        }
    }

    [HttpPut("participants/{privateSessionAthleteId:int}")]
    public async Task<IActionResult> Set(int sessionId, int privateSessionAthleteId, [FromBody] PrivateAttendanceSetRequest request)
    {
        try
        {
            var result = await _privateAttendanceService.SetAsync(sessionId, privateSessionAthleteId, request, IsAdministrator, _currentUser.CoachId, _currentUser.UserId!.Value);

            if (result.NotFound)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมส่วนตัวที่ต้องการ"));
            }

            if (result.Forbidden)
            {
                return Forbid();
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<PrivateAttendanceActionResult>(result, "บันทึกข้อมูลการเข้าร่วมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{SessionId}/private-attendance/participants/{PrivateSessionAthleteId} Controller: PrivateAttendanceController Function: Set UserId: {UserId}", sessionId, privateSessionAthleteId, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถบันทึกข้อมูลการเข้าร่วมได้"));
        }
    }
}
