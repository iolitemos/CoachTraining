using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Athletes;

/// <summary>AthleteCode is immutable after creation — it is snapshotted onto historical
/// Attendance/PrivateSessionAthlete records and used as the business identifier.</summary>
public class AthleteUpdateDto
{
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

public class AthleteStatusUpdateDto
{
    public bool IsActive { get; set; }
}

/// <summary>Lightweight option for Routine attendance selection and Private
/// Training athlete assignment (todo.md 4.2) — active athletes only.</summary>
public class AthleteOptionDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public AthleteType AthleteType { get; set; }
}
