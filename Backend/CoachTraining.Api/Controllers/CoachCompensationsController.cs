using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Compensations;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/coach-compensations")]
[Authorize(Roles = Roles.Administrator)]
public class CoachCompensationsController : ControllerBase
{
    private readonly ICoachCompensationService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachCompensationsController> _logger;

    public CoachCompensationsController(ICoachCompensationService service, ICurrentUserService currentUser, ILogger<CoachCompensationsController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet("statements")]
    public async Task<IActionResult> Get([FromQuery] CoachCompensationPeriodRequest request)
    {
        var result = await _service.GetAsync(request.CoachId, request.Year, request.Month);
        return result is null
            ? NotFound(new ApiErrorResponse("ยังไม่มีสรุปค่าตอบแทนของโค้ชในเดือนนี้"))
            : Ok(new ApiResponse<CoachCompensationStatementDto>(result));
    }

    [HttpPost("statements/calculate")]
    public async Task<IActionResult> Calculate([FromBody] CoachCompensationPeriodRequest request) =>
        await ExecuteAsync(
            () => _service.CalculateAsync(request, _currentUser.UserId!.Value),
            "คำนวณค่าตอบแทนสำเร็จ",
            "Calculate");

    [HttpPut("statements/{id:int}/adjustment")]
    public async Task<IActionResult> UpdateAdjustment(int id, [FromBody] CoachCompensationAdjustmentRequest request) =>
        await ExecuteAsync(
            () => _service.UpdateAdjustmentAsync(id, request, _currentUser.UserId!.Value),
            "บันทึกรายการปรับเพิ่ม/ลดสำเร็จ",
            "UpdateAdjustment");

    [HttpPost("statements/{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id) =>
        await ExecuteAsync(
            () => _service.ConfirmAsync(id, _currentUser.UserId!.Value),
            "ยืนยันสรุปค่าตอบแทนสำเร็จ",
            "Confirm");

    [HttpPost("statements/{id:int}/paid")]
    public async Task<IActionResult> MarkPaid(int id) =>
        await ExecuteAsync(
            () => _service.MarkPaidAsync(id, _currentUser.UserId!.Value),
            "บันทึกการจ่ายค่าตอบแทนสำเร็จ",
            "MarkPaid");

    private async Task<IActionResult> ExecuteAsync(
        Func<Task<CoachCompensationActionResult>> action,
        string successMessage,
        string functionName)
    {
        try
        {
            var result = await action();
            if (result.NotFound)
            {
                return NotFound(new ApiErrorResponse("ไม่พบข้อมูลที่ต้องการ"));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<CoachCompensationStatementDto>(result.Data!, successMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach-compensations Controller: CoachCompensationsController Function: {FunctionName} UserId: {UserId}", functionName, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถดำเนินการค่าตอบแทนได้"));
        }
    }
}
