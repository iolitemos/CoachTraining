using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.CalendarNotes;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/calendar-notes")]
[Authorize]
public class CalendarNotesController : ControllerBase
{
    private readonly ICalendarNoteService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CalendarNotesController> _logger;

    public CalendarNotesController(ICalendarNoteService service, ICurrentUserService currentUser, ILogger<CalendarNotesController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer},{Roles.Coach}")]
    public async Task<IActionResult> List([FromQuery] CalendarNoteRangeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate is null || request.EndDate is null || request.EndDate < request.StartDate
            || request.EndDate.Value.DayNumber - request.StartDate.Value.DayNumber > 62)
            return BadRequest(new ApiErrorResponse("ช่วงวันที่ต้องถูกต้องและไม่เกิน 63 วัน"));

        return Ok(new ApiResponse<IReadOnlyList<CalendarNoteDto>>(
            await _service.ListAsync(request.StartDate.Value, request.EndDate.Value, cancellationToken)));
    }

    [HttpPut("{noteDate}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Upsert(DateOnly noteDate, [FromBody] CalendarNoteUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new ApiErrorResponse("กรุณากรอก Note", [new ApiFieldError("content", "กรุณากรอก Note")]));

        try
        {
            var result = await _service.UpsertAsync(noteDate, request.Content, _currentUser.UserId!.Value, cancellationToken);
            return Ok(new ApiResponse<CalendarNoteDto>(result, "บันทึก Note สำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/calendar-notes/{NoteDate} Controller: CalendarNotesController Function: Upsert UserId: {UserId}", noteDate, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถบันทึก Note ได้"));
        }
    }

    [HttpDelete("{noteDate}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(DateOnly noteDate, CancellationToken cancellationToken)
    {
        try
        {
            return await _service.DeleteAsync(noteDate, _currentUser.UserId!.Value, cancellationToken)
                ? NoContent()
                : NotFound(new ApiErrorResponse("ไม่พบ Note ที่ต้องการ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/calendar-notes/{NoteDate} Controller: CalendarNotesController Function: Delete UserId: {UserId}", noteDate, _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถลบ Note ได้"));
        }
    }
}
