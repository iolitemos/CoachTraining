using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Athletes;

public class AthleteCreateDto
{
    [Required(ErrorMessage = "กรุณากรอกรหัสนักกีฬา")]
    [MaxLength(30, ErrorMessage = "รหัสนักกีฬาต้องไม่เกิน 30 ตัวอักษร")]
    public string AthleteCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? ParentName { get; set; }

    [MaxLength(30)]
    public string? ParentPhoneNumber { get; set; }

    [MaxLength(100)]
    public string? AthleteLevel { get; set; }

    public DateOnly? JoinDate { get; set; }

    public string? Remarks { get; set; }
}
