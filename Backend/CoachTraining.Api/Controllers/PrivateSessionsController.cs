using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.PrivateSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Private Training Management (requirement.md 4.5, todo.md 4.4) — Administrator only.</summary>
[ApiController]
[Route("api/private-sessions")]
[Authorize(Roles = Roles.Administrator)]
public class PrivateSessionsController : ControllerBase
{
    private readonly IPrivateSessionService _privateSessionService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PrivateSessionsController> _logger;

    public PrivateSessionsController(IPrivateSessionService privateSessionService, ICurrentUserService currentUser, ILogger<PrivateSessionsController> logger)
    {
        _privateSessionService = privateSessionService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        try
        {
            var result = await _privateSessionService.ListAsync(request);
            return Ok(new ApiResponse<PagedResponse<PrivateSessionListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/private-sessions Controller: PrivateSessionsController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายการฝึกซ้อมส่วนตัวได้"));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var session = await _privateSessionService.GetByIdAsync(id);
        if (session is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมส่วนตัวที่ต้องการ"));
        }

        return Ok(new ApiResponse<PrivateSessionDetailDto>(session));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PrivateSessionCreateDto dto)
    {
        try
        {
            var result = await _privateSessionService.CreateAsync(dto, _currentUser.UserId!.Value);

            if (result.Conflicts.Count > 0)
            {
                return Conflict(ToConflictResponse(result.Error!, result.Conflicts));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Session!.TrainingSessionId },
                new ApiResponse<PrivateSessionDetailDto>(result.Session, "สร้างเซสชันฝึกซ้อมส่วนตัวสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/private-sessions Controller: PrivateSessionsController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างเซสชันฝึกซ้อมส่วนตัวได้"));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PrivateSessionUpdateDto dto)
    {
        var result = await _privateSessionService.UpdateAsync(id, dto, _currentUser.UserId!.Value);

        if (result.Conflicts.Count > 0)
        {
            return Conflict(ToConflictResponse(result.Error!, result.Conflicts));
        }

        if (result.Error is not null)
        {
            return BadRequest(new ApiErrorResponse(result.Error));
        }

        if (result.Session is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมส่วนตัวที่ต้องการ"));
        }

        return Ok(new ApiResponse<PrivateSessionDetailDto>(result.Session, "บันทึกข้อมูลสำเร็จ"));
    }

    private static ApiErrorResponse ToConflictResponse(string message, List<ConflictDetail> conflicts) =>
        new(message, conflicts.Select(c => new ApiFieldError(c.AthleteId is not null ? "athleteIds" : "coachId", c.Message)).ToList());
}
