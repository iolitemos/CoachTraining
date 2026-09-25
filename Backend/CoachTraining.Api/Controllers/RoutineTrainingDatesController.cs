using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.ParentRoutinePlans;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/routine-training-dates")]
[Authorize(Roles = Roles.Administrator)]
public class RoutineTrainingDatesController : ControllerBase
{
    private readonly IRoutineTrainingDateService _service;
    private readonly ICurrentUserService _currentUser;

    public RoutineTrainingDatesController(IRoutineTrainingDateService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        if (startDate is null || endDate is null || endDate < startDate || endDate.Value.DayNumber - startDate.Value.DayNumber > 62)
            return BadRequest(new ApiErrorResponse("ช่วงวันที่ต้องถูกต้องและไม่เกิน 63 วัน"));
        return Ok(new ApiResponse<IReadOnlyList<RoutineTrainingDateDto>>(await _service.ListAsync(startDate.Value, endDate.Value, cancellationToken)));
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] RoutineTrainingDateCreateDto request, CancellationToken cancellationToken)
    {
        if (request.TrainingDate is null) return BadRequest(new ApiErrorResponse("กรุณาระบุวันที่ฝึกซ้อมประจำ"));
        var result = await _service.AddAsync(request.TrainingDate.Value, _currentUser.UserId!.Value, cancellationToken);
        return Ok(new ApiResponse<RoutineTrainingDateDto>(result, "เพิ่มวันฝึกซ้อมประจำแล้ว"));
    }

    [HttpDelete("{trainingDate}")]
    public async Task<IActionResult> Remove(DateOnly trainingDate, CancellationToken cancellationToken)
    {
        return await _service.RemoveAsync(trainingDate, _currentUser.UserId!.Value, cancellationToken)
            ? NoContent()
            : NotFound(new ApiErrorResponse("ไม่พบวันฝึกซ้อมประจำ"));
    }
}
