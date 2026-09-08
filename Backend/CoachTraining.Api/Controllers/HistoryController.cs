using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.History;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>
/// Session business history / audit trail (requirement.md 6.20, todo.md 4.20) —
/// every signed-in role may call this; a Coach account only ever sees history for
/// its own sessions (CLAUDE.md section 11), enforced in the service layer.
/// </summary>
[ApiController]
[Route("api/training-sessions/{id:int}/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(IAuditService auditService, ICurrentUserService currentUser, ILogger<HistoryController> logger)
    {
        _auditService = auditService;
        _currentUser = currentUser;
        _logger = logger;
    }

    private bool IsPrivilegedRole => _currentUser.IsInRole(Roles.Administrator) || _currentUser.IsInRole(Roles.ManagementViewer);

    [HttpGet]
    public async Task<IActionResult> GetAuditLog(int id)
    {
        try
        {
            var result = await _auditService.GetSessionAuditLogAsync(id, IsPrivilegedRole, _currentUser.CoachId);
            return ToActionResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/history Controller: HistoryController Function: GetAuditLog UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดประวัติเซสชันได้"));
        }
    }

    [HttpGet("approvals")]
    public async Task<IActionResult> GetApprovalHistory(int id)
    {
        try
        {
            var result = await _auditService.GetApprovalHistoryAsync(id, IsPrivilegedRole, _currentUser.CoachId);
            return ToActionResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/history/approvals Controller: HistoryController Function: GetApprovalHistory UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดประวัติการอนุมัติได้"));
        }
    }

    [HttpGet("substitutions")]
    public async Task<IActionResult> GetSubstitutionHistory(int id)
    {
        try
        {
            var result = await _auditService.GetSubstitutionHistoryAsync(id, IsPrivilegedRole, _currentUser.CoachId);
            return ToActionResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/history/substitutions Controller: HistoryController Function: GetSubstitutionHistory UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดประวัติการเปลี่ยนโค้ชได้"));
        }
    }

    [HttpGet("conflict-overrides")]
    public async Task<IActionResult> GetConflictOverrideHistory(int id)
    {
        try
        {
            var result = await _auditService.GetConflictOverrideHistoryAsync(id, IsPrivilegedRole, _currentUser.CoachId);
            return ToActionResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/history/conflict-overrides Controller: HistoryController Function: GetConflictOverrideHistory UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดประวัติการยืนยันตารางทับซ้อนได้"));
        }
    }

    private IActionResult ToActionResult<T>(HistoryAccessResult<T> result)
    {
        if (result.NotFound)
        {
            return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
        }

        if (result.Forbidden)
        {
            return Forbid();
        }

        return Ok(new ApiResponse<T>(result.Data!));
    }
}
