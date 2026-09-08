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

    [Required(ErrorMessage = "กรุณาเลือกสีประจำตัวโค้ช")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "รูปแบบสีประจำตัวโค้ชไม่ถูกต้อง")]
    public string ColorHex { get; set; } = "#10B981";

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(30)]
    public string? BankAccountNumber { get; set; }

    [MaxLength(200)]
    public string? BankAccountName { get; set; }

    public string? Remarks { get; set; }
}
