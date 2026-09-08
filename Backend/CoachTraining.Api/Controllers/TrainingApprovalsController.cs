using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Approvals;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Submit/Approve/Reject/RequestRevision/Unlock workflow (requirement.md 6.15, todo.md 4.15).
/// Submit is available to the acting Coach or an Administrator; every other action is
/// Administrator-only (FR-APPROVAL-002/005).</summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize(Roles = $"{Roles.Coach},{Roles.Administrator}")]
public class TrainingApprovalsController : ControllerBase
{
    private readonly ITrainingApprovalService _approvalService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TrainingApprovalsController> _logger;

    public TrainingApprovalsController(ITrainingApprovalService approvalService, ICurrentUserService currentUser, ILogger<TrainingApprovalsController> logger)
    {
        _approvalService = approvalService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        try
        {
            var result = await _approvalService.SubmitAsync(id, _currentUser.IsInRole(Roles.Administrator), _currentUser.CoachId, _currentUser.UserId!.Value);
            return ToActionResult(result, "ส่งตรวจสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/submit Controller: TrainingApprovalsController Function: Submit UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถส่งตรวจได้"));
        }
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalCommentRequest request)
    {
        try
        {
            var result = await _approvalService.ApproveAsync(id, request, _currentUser.UserId!.Value);
            return ToActionResult(result, "อนุมัติและล็อกเซสชันสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/approve Controller: TrainingApprovalsController Function: Approve UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถอนุมัติเซสชันได้"));
        }
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Reject(int id, [FromBody] ApprovalReasonRequest request)
    {
        try
        {
            var result = await _approvalService.RejectAsync(id, request, _currentUser.UserId!.Value);
            return ToActionResult(result, "ตีกลับเซสชันสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/reject Controller: TrainingApprovalsController Function: Reject UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถตีกลับเซสชันได้"));
        }
    }

    [HttpPost("{id:int}/request-revision")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> RequestRevision(int id, [FromBody] ApprovalReasonRequest request)
    {
        try
        {
            var result = await _approvalService.RequestRevisionAsync(id, request, _currentUser.UserId!.Value);
            return ToActionResult(result, "ส่งคำขอแก้ไขสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/request-revision Controller: TrainingApprovalsController Function: RequestRevision UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถส่งคำขอแก้ไขได้"));
        }
    }

    [HttpPost("{id:int}/unlock")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Unlock(int id, [FromBody] ApprovalReasonRequest request)
    {
        try
        {
            var result = await _approvalService.UnlockAsync(id, request, _currentUser.UserId!.Value);
            return ToActionResult(result, "ปลดล็อกเซสชันสำเร็จ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/training-sessions/{Id}/unlock Controller: TrainingApprovalsController Function: Unlock UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถปลดล็อกเซสชันได้"));
        }
    }

    private IActionResult ToActionResult(ApprovalActionResult result, string successMessage)
    {
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

        return Ok(new ApiResponse<TrainingApprovalActionResponseDto>(
            new TrainingApprovalActionResponseDto { Session = result.Session!, History = result.History! },
            successMessage));
    }
}
