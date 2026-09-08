using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Services;
using CoachTraining.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IPasswordService passwordService, ICurrentUserService currentUser, ILogger<AuthController> logger)
    {
        _authService = authService;
        _passwordService = passwordService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            if (result is null)
            {
                return Unauthorized(new ApiErrorResponse("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง"));
            }

            return Ok(new ApiResponse<LoginResponse>(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during login. Route: api/auth/login Controller: AuthController Function: Login");
            return StatusCode(500, new ApiErrorResponse("เกิดข้อผิดพลาด ไม่สามารถเข้าสู่ระบบได้ในขณะนี้"));
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int userId)
        {
            return Unauthorized(new ApiErrorResponse("กรุณาเข้าสู่ระบบใหม่"));
        }

        var error = await _passwordService.ChangePasswordAsync(userId, request, cancellationToken);
        return error is null
            ? Ok(new ApiResponse<object?>(null, "เปลี่ยนรหัสผ่านสำเร็จ"))
            : BadRequest(new ApiErrorResponse(error));
    }

    [AllowAnonymous]
    [EnableRateLimiting("ForgotPassword")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _passwordService.RequestResetAsync(request, cancellationToken);
        return Ok(new ApiResponse<object?>(null, "หากอีเมลนี้ลงทะเบียนอยู่ในระบบ เราได้ส่งลิงก์ตั้งรหัสผ่านใหม่ให้แล้ว"));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var error = await _passwordService.ResetPasswordAsync(request, cancellationToken);
        return error is null
            ? Ok(new ApiResponse<object?>(null, "ตั้งรหัสผ่านใหม่สำเร็จ"))
            : BadRequest(new ApiErrorResponse(error));
    }
}
