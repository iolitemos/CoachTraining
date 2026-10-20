using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.DTOs.RoutineScheduleRequests;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Restricted Coach self-service creation for the signed-in Coach's own
/// Routine Training schedules.</summary>
[ApiController]
[Route("api/coach/routine-schedules")]
[Authorize(Roles = Roles.Coach)]
public class CoachRoutineSchedulesController : ControllerBase
{
    private readonly IRoutineScheduleService _routineScheduleService;
    private readonly IRoutineScheduleRequestService _requestService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachRoutineSchedulesController> _logger;

    public CoachRoutineSchedulesController(
        IRoutineScheduleService routineScheduleService,
        IRoutineScheduleRequestService requestService,
        ICurrentUserService currentUser,
        ILogger<CoachRoutineSchedulesController> logger)
    {
        _routineScheduleService = routineScheduleService;
        _requestService = requestService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CoachRoutineScheduleCreateDto dto)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var result = await _requestService.CreateAsync(coachId, new RoutineScheduleRequestCreateDto
            {
                ProposedStartTime = dto.StartTime,
                ProposedEndTime = dto.EndTime,
                ProposedDate = dto.EffectiveStartDate,
                ProposedRemarks = dto.Remarks,
            }, userId);

            if (result.Conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    result.Error!,
                    result.Conflicts.Select(message => new ApiFieldError("schedule", message)).ToList()));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return StatusCode(201, new ApiResponse<RoutineScheduleRequestDto>(result.Request!, "ส่งคำขอตารางฝึกซ้อมเพื่อรอผู้ดูแลอนุมัติแล้ว"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules Controller: CoachRoutineSchedulesController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเพิ่มตารางฝึกซ้อมได้"));
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch([FromBody] CoachRoutineScheduleBatchCreateDto dto)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var (requests, error, conflicts) = await _requestService.CreateBatchAsync(coachId, new RoutineScheduleRequestBatchCreateDto
            {
                StartDate = dto.StartDate, EndDate = dto.EndDate, StartTime = dto.StartTime,
                EndTime = dto.EndTime, DaysOfWeek = dto.DaysOfWeek, Remarks = dto.Remarks,
            }, userId);
            if (conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    error!, conflicts.Select(message => new ApiFieldError("schedule", message)).ToList()));
            }

            if (error is not null)
            {
                return BadRequest(new ApiErrorResponse(error));
            }

            var result = new CoachRoutineScheduleBatchCreateResult
            {
                CreatedCount = requests.Count,
                CreatedDates = requests.Select(x => x.ProposedDate).ToList(),
            };
            return StatusCode(201, new ApiResponse<CoachRoutineScheduleBatchCreateResult>(
                result, $"ส่งคำขอตารางฝึกซ้อม {result.CreatedCount} รายการเพื่อรอผู้ดูแลอนุมัติแล้ว"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules/batch Controller: CoachRoutineSchedulesController Function: CreateBatch UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเพิ่มตารางฝึกซ้อมได้"));
        }
    }

    [HttpGet("requests")]
    public async Task<IActionResult> ListOwnRequests([FromQuery] DateOnly? dateFrom = null, [FromQuery] DateOnly? dateTo = null)
    {
        if (_currentUser.CoachId is not int coachId) return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        if (dateFrom is not null && dateTo is not null && dateTo < dateFrom)
            return BadRequest(new ApiErrorResponse("ช่วงวันที่ไม่ถูกต้อง"));
        return Ok(new ApiResponse<List<RoutineScheduleRequestDto>>(await _requestService.ListOwnAsync(coachId, dateFrom, dateTo)));
    }

    [HttpPut("requests/{id:int}")]
    public async Task<IActionResult> UpdateOwnRequest(int id, [FromBody] CoachRoutineScheduleCreateDto dto)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        var result = await _requestService.UpdateOwnAsync(id, coachId, new RoutineScheduleRequestCreateDto
        {
            ProposedDate = dto.EffectiveStartDate, ProposedStartTime = dto.StartTime,
            ProposedEndTime = dto.EndTime, ProposedRemarks = dto.Remarks,
        }, userId);
        return ToRequestActionResult(result, "แก้ไขและส่งคำขอกลับเพื่อตรวจสอบแล้ว");
    }

    [HttpDelete("requests/{id:int}")]
    public async Task<IActionResult> CancelOwnRequest(int id)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        return ToRequestActionResult(await _requestService.CancelOwnAsync(id, coachId, userId), "ยกเลิกคำขอแล้ว");
    }

    private IActionResult ToRequestActionResult(RoutineScheduleRequestActionResult result, string message)
    {
        if (result.NotFound) return NotFound(new ApiErrorResponse("ไม่พบคำขอตารางฝึกซ้อม"));
        if (result.Conflicts.Count > 0) return Conflict(new ApiErrorResponse(result.Error!, result.Conflicts.Select(x => new ApiFieldError("schedule", x)).ToList()));
        if (result.Error is not null) return BadRequest(new ApiErrorResponse(result.Error));
        return Ok(new ApiResponse<RoutineScheduleRequestDto>(result.Request!, message));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (_currentUser.CoachId is not int coachId || _currentUser.UserId is not int userId)
        {
            return BadRequest(new ApiErrorResponse("บัญชีนี้ไม่ได้เชื่อมโยงกับข้อมูลโค้ช"));
        }

        try
        {
            var result = await _routineScheduleService.DeleteOwnAsync(id, coachId, userId);
            if (!result.Found)
            {
                return NotFound(new ApiErrorResponse("ไม่พบตารางฝึกซ้อมที่ต้องการ"));
            }

            if (result.Forbidden)
            {
                return Forbid();
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<object>(new { }, "ลบตารางฝึกซ้อมสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coach/routine-schedules/{Id} Controller: CoachRoutineSchedulesController Function: Delete UserId: {UserId}", id, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถลบตารางฝึกซ้อมได้"));
        }
    }
}
