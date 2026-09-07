using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoachTraining.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
}
