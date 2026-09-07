using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    public string Password { get; set; } = string.Empty;
}
