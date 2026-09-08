using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.TrainingSessions;

namespace CoachTraining.Api.DTOs.Reschedules;

/// <summary>New date/time for a replacement session (FR-CR-004–007). The coach
/// assignment itself never changes here — reassigning who teaches is Substitute
/// Coach's (4.11) concern, not Rescheduling's.</summary>
public class RescheduleSessionRequest : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกวันที่ฝึกซ้อมใหม่")]
    public DateOnly SessionDate { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    public string? Remarks { get; set; }

    /// <summary>FR-CONFLICT-004 — Administrator confirms proceeding despite a detected coach/athlete conflict.</summary>
    public bool OverrideConflict { get; set; }

    /// <summary>Required when <see cref="OverrideConflict"/> is true (FR-CONFLICT-004/005).</summary>
    [MaxLength(1000, ErrorMessage = "เหตุผลต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string? OverrideReason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        }

        if (OverrideConflict && string.IsNullOrWhiteSpace(OverrideReason))
        {
            yield return new ValidationResult("กรุณาระบุเหตุผลในการยืนยันดำเนินการทั้งที่มีตารางทับซ้อน", [nameof(OverrideReason)]);
        }
    }
}

/// <summary>Service-layer result translated to HTTP semantics by the thin controller.</summary>
public class RescheduleActionResult
{
    /// <summary>The original session, now in Rescheduled status (FR-SESSION-010, FR-CR-006).</summary>
    public TrainingSessionDetailDto? OriginalSession { get; set; }

    /// <summary>The new session created for the replacement date/time, linked back via OriginalSessionId.</summary>
    public TrainingSessionDetailDto? ReplacementSession { get; set; }

    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public List<ConflictDetail> Conflicts { get; set; } = [];
}

/// <summary>API response body for a successful reschedule action.</summary>
public class RescheduleResponseDto
{
    public TrainingSessionDetailDto OriginalSession { get; set; } = null!;
    public TrainingSessionDetailDto ReplacementSession { get; set; } = null!;
}
