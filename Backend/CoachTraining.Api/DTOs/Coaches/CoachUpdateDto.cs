using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.Coaches;

/// <summary>CoachCode is immutable after creation — it is snapshotted onto historical
/// TrainingSession records and used as the athlete/coach-facing business identifier.</summary>
public class CoachUpdateDto
{
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

public class CoachStatusUpdateDto
{
    public bool IsActive { get; set; }
}
