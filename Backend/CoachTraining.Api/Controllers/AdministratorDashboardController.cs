using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Dashboards;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Administrator Dashboard (requirement.md 6.17, todo.md 4.17) — Administrator and
/// Management/Viewer (read-only, CLAUDE.md section 11).</summary>
[ApiController]
[Route("api/dashboard/administrator")]
[Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer}")]
public class AdministratorDashboardController : ControllerBase
{
    private readonly IAdministratorDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdministratorDashboardController> _logger;

    public AdministratorDashboardController(IAdministratorDashboardService dashboardService, ICurrentUserService currentUser, ILogger<AdministratorDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AdministratorDashboardFilterRequest filter)
    {
        try
        {
            var result = await _dashboardService.GetDashboardAsync(filter);
            return Ok(new ApiResponse<AdministratorDashboardResponseDto>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/dashboard/administrator Controller: AdministratorDashboardController Function: Get UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดข้อมูลแดชบอร์ดได้"));
        }
    }
}
