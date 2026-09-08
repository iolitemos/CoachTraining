using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Substitutions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>Administrator-only substitute-coach actions (FR-SUB-001–005).</summary>
[ApiController]
[Route("api/training-sessions")]
[Authorize(Roles = Roles.Administrator)]
public class SubstituteCoachesController : ControllerBase
{
    private readonly ISubstituteCoachService _substituteCoachService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SubstituteCoachesController> _logger;

    public SubstituteCoachesController(
        ISubstituteCoachService substituteCoachService,
        ICurrentUserService currentUser,
        ILogger<SubstituteCoachesController> logger)
    {
        _substituteCoachService = substituteCoachService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("{id:int}/substitute")]
    public async Task<IActionResult> Assign(int id, [FromBody] SubstituteCoachRequest request)
    {
        try
        {
            var result = await _substituteCoachService.AssignAsync(id, request, _currentUser.UserId!.Value);

            if (result.NotFound)
            {
                return NotFound(new ApiErrorResponse("ไม่พบเซสชันฝึกซ้อมที่ต้องการ"));
            }

            if (result.Conflicts.Count > 0)
            {
                return Conflict(new ApiErrorResponse(
                    result.Error!,
                    result.Conflicts
                        .Select(c => new ApiFieldError("substituteCoachId", c.Message))
                        .ToList()));
            }

            if (result.Error is not null)
            {
                return BadRequest(new ApiErrorResponse(result.Error));
            }

            return Ok(new ApiResponse<SubstituteCoachResponseDto>(result.Data!, "กำหนดโค้ชตัวแทนสำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Route: api/training-sessions/{Id}/substitute Controller: SubstituteCoachesController Function: Assign UserId: {UserId}",
                id,
                _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถกำหนดโค้ชตัวแทนได้"));
        }
    }
}
