using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.CompetitionMatches;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/competition-matches")]
[Authorize]
public class CompetitionMatchesController : ControllerBase
{
    private readonly ICompetitionMatchService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CompetitionMatchesController> _logger;

    public CompetitionMatchesController(ICompetitionMatchService service, ICurrentUserService currentUser, ILogger<CompetitionMatchesController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Administrator},{Roles.ManagementViewer},{Roles.Coach}")]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        try { return Ok(new ApiResponse<PagedResponse<CompetitionMatchDto>>(await _service.ListAsync(request))); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/competition-matches Controller: CompetitionMatchesController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายการแข่งขันได้"));
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? NotFound(new ApiErrorResponse("ไม่พบรายการแข่งขันที่ต้องการ"))
            : Ok(new ApiResponse<CompetitionMatchDto>(result));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Create([FromBody] CompetitionMatchRequestDto dto)
    {
        var result = await _service.CreateAsync(dto, _currentUser.UserId!.Value);
        return CreatedAtAction(nameof(GetById), new { id = result.CompetitionMatchId }, new ApiResponse<CompetitionMatchDto>(result, "เพิ่มรายการแข่งขันสำเร็จ"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Update(int id, [FromBody] CompetitionMatchRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, _currentUser.UserId!.Value);
        return result is null
            ? NotFound(new ApiErrorResponse("ไม่พบรายการแข่งขันที่ต้องการ"))
            : Ok(new ApiResponse<CompetitionMatchDto>(result, "บันทึกรายการแข่งขันสำเร็จ"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id, _currentUser.UserId!.Value);
        return deleted ? NoContent() : NotFound(new ApiErrorResponse("ไม่พบรายการแข่งขันที่ต้องการ"));
    }
}
