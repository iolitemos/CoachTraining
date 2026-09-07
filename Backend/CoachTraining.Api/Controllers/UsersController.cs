using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Users;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

/// <summary>User & Role Management (requirement.md 4.1, todo.md 3.2) — Administrator only.</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Administrator)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ICurrentUserService currentUser, ILogger<UsersController> logger)
    {
        _userService = userService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        try
        {
            var result = await _userService.ListAsync(request);
            return Ok(new ApiResponse<PagedResponse<UserListItemDto>>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/users Controller: UsersController Function: List UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถโหลดรายชื่อผู้ใช้ได้"));
        }
    }

    [HttpGet("role-options")]
    public async Task<IActionResult> GetRoleOptions()
    {
        var options = await _userService.GetRoleOptionsAsync();
        return Ok(new ApiResponse<List<RoleOptionDto>>(options));
    }

    [HttpGet("coach-options")]
    public async Task<IActionResult> GetCoachOptions()
    {
        var options = await _userService.GetCoachOptionsAsync();
        return Ok(new ApiResponse<List<CoachOptionDto>>(options));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบผู้ใช้ที่ต้องการ"));
        }

        return Ok(new ApiResponse<UserDetailDto>(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
    {
        try
        {
            var (result, error) = await _userService.CreateAsync(dto, _currentUser.UserId!.Value);
            if (error is not null)
            {
                return BadRequest(new ApiErrorResponse(error));
            }

            return CreatedAtAction(nameof(GetById), new { id = result!.UserId }, new ApiResponse<UserDetailDto>(result, "สร้างผู้ใช้สำเร็จ"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Route: api/users Controller: UsersController Function: Create UserId: {UserId}", _currentUser.UserId);
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถสร้างผู้ใช้ได้"));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        var (result, error) = await _userService.UpdateAsync(id, dto, _currentUser.UserId!.Value);
        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (result is null)
        {
            return NotFound(new ApiErrorResponse("ไม่พบผู้ใช้ที่ต้องการ"));
        }

        return Ok(new ApiResponse<UserDetailDto>(result, "บันทึกข้อมูลสำเร็จ"));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] UserStatusUpdateDto dto)
    {
        var success = await _userService.SetStatusAsync(id, dto.IsActive, _currentUser.UserId!.Value);
        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบผู้ใช้ที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, dto.IsActive ? "เปิดใช้งานบัญชีสำเร็จ" : "ปิดใช้งานบัญชีสำเร็จ"));
    }

    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> AssignRoles(int id, [FromBody] AssignRolesDto dto)
    {
        var (success, error) = await _userService.AssignRolesAsync(id, dto.RoleIds, _currentUser.UserId!.Value);
        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบผู้ใช้ที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, "บันทึกบทบาทสำเร็จ"));
    }

    [HttpPut("{id:int}/coach-link")]
    public async Task<IActionResult> SetCoachLink(int id, [FromBody] CoachLinkDto dto)
    {
        var (success, error) = await _userService.SetCoachLinkAsync(id, dto.CoachId, _currentUser.UserId!.Value);
        if (error is not null)
        {
            return BadRequest(new ApiErrorResponse(error));
        }

        if (!success)
        {
            return NotFound(new ApiErrorResponse("ไม่พบผู้ใช้ที่ต้องการ"));
        }

        return Ok(new ApiResponse<object>(new { }, "บันทึกการเชื่อมโยงโค้ชสำเร็จ"));
    }
}
