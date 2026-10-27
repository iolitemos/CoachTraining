using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineScheduleRequests;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/routine-schedule-requests")]
[Authorize(Roles = Roles.Administrator)]
public class RoutineScheduleRequestsController : ControllerBase
{
    private readonly IRoutineScheduleRequestService _service;
    private readonly ICurrentUserService _currentUser;

    public RoutineScheduleRequestsController(IRoutineScheduleRequestService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RoutineScheduleRequestStatus? status = null) =>
        Ok(new ApiResponse<List<RoutineScheduleRequestDto>>(await _service.ListAsync(status)));

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] RoutineScheduleRequestReviewDto dto) =>
        ToActionResult(await _service.ApproveAsync(id, dto, _currentUser.UserId!.Value), "อนุมัติและสร้างตารางฝึกซ้อมสำเร็จ");

    [HttpPost("batch-approve")]
    public async Task<IActionResult> ApproveBatch([FromBody] RoutineScheduleRequestBatchApproveDto dto)
    {
        var result = await _service.ApproveBatchAsync(dto, _currentUser.UserId!.Value);
        if (result.NotFound) return NotFound(new ApiErrorResponse(result.Error ?? "ไม่พบคำขอตารางฝึกซ้อมบางรายการ"));
        if (result.Conflicts.Count > 0)
            return Conflict(new ApiErrorResponse(result.Error!, result.Conflicts.Select(x => new ApiFieldError("schedule", x)).ToList()));
        if (result.Error is not null) return BadRequest(new ApiErrorResponse(result.Error));
        return Ok(new ApiResponse<List<RoutineScheduleRequestDto>>(result.Requests, $"อนุมัติคำขอ {result.Requests.Count} รายการสำเร็จ"));
    }

    [HttpPost("{id:int}/request-revision")]
    public async Task<IActionResult> RequestRevision(int id, [FromBody] RoutineScheduleRequestDecisionDto dto) =>
        ToActionResult(await _service.RequestRevisionAsync(id, dto, _currentUser.UserId!.Value), "ส่งคำขอกลับให้โค้ชแก้ไขแล้ว");

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RoutineScheduleRequestDecisionDto dto) =>
        ToActionResult(await _service.RejectAsync(id, dto, _currentUser.UserId!.Value), "ปฏิเสธคำขอแล้ว");

    [HttpDelete("{id:int}/approved-schedule")]
    public async Task<IActionResult> DeleteApprovedSchedule(int id) =>
        ToActionResult(await _service.DeleteApprovedAsync(id, _currentUser.UserId!.Value), "ลบตารางฝึกซ้อมที่อนุมัติแล้วสำเร็จ");

    private IActionResult ToActionResult(RoutineScheduleRequestActionResult result, string message)
    {
        if (result.NotFound) return NotFound(new ApiErrorResponse("ไม่พบคำขอตารางฝึกซ้อม"));
        if (result.Conflicts.Count > 0) return Conflict(new ApiErrorResponse(result.Error!, result.Conflicts.Select(x => new ApiFieldError("schedule", x)).ToList()));
        if (result.Error is not null) return BadRequest(new ApiErrorResponse(result.Error));
        return Ok(new ApiResponse<RoutineScheduleRequestDto>(result.Request!, message));
    }
}
