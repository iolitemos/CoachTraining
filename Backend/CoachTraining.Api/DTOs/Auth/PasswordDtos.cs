using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "โทเคนไม่ถูกต้อง")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [MinLength(8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    public string NewPassword { get; set; } = string.Empty;
}
