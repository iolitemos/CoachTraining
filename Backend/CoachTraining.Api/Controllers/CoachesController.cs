using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Coach Management (requirement.md 4.2, todo.md 4.1) — Administrator only.</summary>
[ApiController]
[Route("api/coaches")]
[Authorize]
public class CoachesController : ControllerBase
{
    private readonly ICoachService _coachService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoachesController> _logger;

    public CoachesController(ICoachService coachService, ICurrentUserService currentUser, ILogger<CoachesController> logger)
    {
        _coachService = coachService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        try
        {
            var result = await _coachService.ListAsync(request);
            return Ok(new ApiResponse<PagedResponse<CoachListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coaches Controller: CoachesController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายชื่อโค้ชได้"));
        }
    }

    [HttpGet("options")]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer},{Roles.Coach}")]
    public async Task<IActionResult> GetActiveOptions()
    {
        var options = await _coachService.GetActiveOptionsAsync();
        return Ok(new ApiResponse<List<CoachOptionDto>>(options));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetById(int id)
    {
        var coach = await _coachService.GetByIdAsync(id);
        if (coach is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลโค้ชที่ต้องการ"));
        }

        return Ok(new ApiResponse<CoachDetailDto>(coach));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([FromBody] CoachCreateDto dto)
    {
        try
        {
            var (result, error) = await _coachService.CreateAsync(dto, _currentUser.UserId!.Value);
            if (error is not null)
            {
                return BadRequest(new ApiErrorResponse(error));
            }

            return CreatedAtAction(nameof(GetById), new { id = result!.CoachId }, new ApiResponse<CoachDetailDto>(result, "สร้างข้อมูลโค้ชสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/coaches Controller: CoachesController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างข้อมูลโค้ชได้"));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Update(int id, [FromBody] CoachUpdateDto dto)
    {
        var (result, error) = await _coachService.UpdateAsync(id, dto, _currentUser.UserId!.Value);
        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (result is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลโค้ชที่ต้องการ"));
        }

        return Ok(new ApiResponse<CoachDetailDto>(result, "บันทึกข้อมูลสำเร็จ"));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> SetStatus(int id, [FromBody] CoachStatusUpdateDto dto)
    {
        var success = await _coachService.SetStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบข้อมูลโค้ชที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, dto.IsActive ? "เปิดใช้งานโค้ชสำเร็จ" : "ปิดใช้งานโค้ชสำเร็จ"));
    }
}
