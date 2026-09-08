using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Dashboards;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Coach Home dashboard (requirement.md 6.16, todo.md 4.16) — always scoped to
/// the signed-in Coach (CLAUDE.md section 11).</summary>
[ApiController]
[Route("api/dashboard/coach")]
[Authorize(Roles = Roles.Coach)]
public class CoachDashboardController : ControllerBase
{
    private readonly ICoachDashboardService _coachDashboardService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachDashboardController> _logger;

    public CoachDashboardController(ICoachDashboardService coachDashboardService, ICurrentUserService currentUser, ILogger<CoachDashboardController> logger)
    {
        _coachDashboardService = coachDashboardService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (_currentUser.CoachId is null)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var result = await _coachDashboardService.GetDashboardAsync(_currentUser.CoachId.Value);
            return Ok(new ApiResponse<CoachDashboardResponseDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/dashboard/coach Controller: CoachDashboardController Function: Get UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดข้อมูลแดชบอร์ดได้"));
        }
    }
}
