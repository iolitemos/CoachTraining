using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Athletes;

public class AthleteCreateDto
{
    [Required(ErrorMessage = "กรุณากรอกรหัสนักกีฬา")]
    [MaxLength(30, ErrorMessage = "รหัสนักกีฬาต้องไม่เกิน 30 ตัวอักษร")]
    public string AthleteCode { get; set; } = string.Empty;

    [EnumDataType(typeof(AthleteType), ErrorMessage = "ประเภทนักกีฬาไม่ถูกต้อง")]
    public AthleteType AthleteType { get; set; } = AthleteType.Affiliated;

    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Nickname { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [Range(1900, 2100, ErrorMessage = "ปีเกิดต้องอยู่ระหว่าง 1900 ถึง 2100")]
    public int? BirthYear { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? ParentName { get; set; }

    [MaxLength(30)]
    public string? ParentPhoneNumber { get; set; }

    [MaxLength(100)]
    public string? AthleteLevel { get; set; }

    public DateOnly? JoinDate { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    public string? Remarks { get; set; }
}
