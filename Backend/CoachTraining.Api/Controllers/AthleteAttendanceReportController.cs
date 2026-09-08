using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Athlete Attendance Report (requirement.md 6.19, todo.md 4.19) — Administrator
/// and Management/Viewer (read-only, CLAUDE.md section 11).</summary>
[ApiController]
[Route("api/reports/athlete-attendance")]
[Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer}")]
public class AthleteAttendanceReportController : ControllerBase
{
    private readonly IAthleteAttendanceReportService _reportService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AthleteAttendanceReportController> _logger;

    public AthleteAttendanceReportController(IAthleteAttendanceReportService reportService, ICurrentUserService currentUser, ILogger<AthleteAttendanceReportController> logger)
    {
        _reportService = reportService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AthleteAttendanceReportFilter filter)
    {
        try
        {
            var result = await _reportService.GetReportAsync(filter);
            return Ok(new ApiResponse<AthleteAttendanceReportResponseDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/reports/athlete-attendance Controller: AthleteAttendanceReportController Function: Get UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายงานการเข้าร่วมได้"));
        }
    }
}
