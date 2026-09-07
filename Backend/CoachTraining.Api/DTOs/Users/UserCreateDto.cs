using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Users;

public class UserCreateDto
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [MaxLength(50, ErrorMessage = "ชื่อผู้ใช้ต้องไม่เกิน 50 ตัวอักษร")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [MinLength(8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกอย่างน้อยหนึ่งบทบาท")]
    [MinLength(1, ErrorMessage = "กรุณาเลือกอย่างน้อยหนึ่งบทบาท")]
    public List<int> RoleIds { get; set; } = [];
}
