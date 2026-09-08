using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Coach Teaching-Hour Report (requirement.md 6.18, todo.md 4.18) — Administrator
/// and Management/Viewer (read-only, CLAUDE.md section 11).</summary>
[ApiController]
[Route("api/reports/coach-teaching-hours")]
[Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer}")]
public class CoachTeachingHourReportController : ControllerBase
{
    private readonly ICoachTeachingHourReportService _reportService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachTeachingHourReportController> _logger;

    public CoachTeachingHourReportController(ICoachTeachingHourReportService reportService, ICurrentUserService currentUser, ILogger<CoachTeachingHourReportController> logger)
    {
        _reportService = reportService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CoachTeachingHourReportFilter filter)
    {
        try
        {
            var result = await _reportService.GetReportAsync(filter);
            return Ok(new ApiResponse<CoachTeachingHourReportResponseDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/reports/coach-teaching-hours Controller: CoachTeachingHourReportController Function: Get UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายงานชั่วโมงสอนได้"));
        }
    }
}
