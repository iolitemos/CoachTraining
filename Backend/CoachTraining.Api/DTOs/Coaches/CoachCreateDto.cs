using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Coaches;

public class CoachCreateDto
{
    [Required(ErrorMessage = "กรุณากรอกรหัสโค้ช")]
    [MaxLength(30, ErrorMessage = "รหัสโค้ชต้องไม่เกิน 30 ตัวอักษร")]
    public string CoachCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? CoachType { get; set; }

    [MaxLength(200)]
    public string? Specialization { get; set; }

    public string? Remarks { get; set; }
}
