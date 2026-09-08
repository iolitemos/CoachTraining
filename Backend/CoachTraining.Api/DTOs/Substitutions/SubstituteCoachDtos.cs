using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.TrainingSessions;

namespace CoachTraining.Api.DTOs.Substitutions;

/// <summary>Assigns an active substitute coach to an eligible session (FR-SUB-001–005).</summary>
public class SubstituteCoachRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกโค้ชตัวแทน")]
    public int SubstituteCoachId { get; set; }

    [Required(ErrorMessage = "กรุณาระบุเหตุผลในการเปลี่ยนโค้ช")]
    [MaxLength(1000, ErrorMessage = "เหตุผลต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>FR-CONFLICT-004 — Administrator confirms proceeding despite a detected schedule conflict.</summary>
    public bool OverrideConflict { get; set; }

    /// <summary>Required when <see cref="OverrideConflict"/> is true (FR-CONFLICT-004/005).</summary>
    [MaxLength(1000, ErrorMessage = "เหตุผลต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string? OverrideReason { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OverrideConflict && string.IsNullOrWhiteSpace(OverrideReason))
        {
            yield return new ValidationResult("กรุณาระบุเหตุผลในการยืนยันดำเนินการทั้งที่มีตารางทับซ้อน", [nameof(OverrideReason)]);
        }
    }
}

/// <summary>Immutable business details recorded for one substitute-coach assignment.</summary>
public class CoachSubstitutionHistoryDto
{
    public int CoachSubstitutionHistoryId { get; set; }
    public int TrainingSessionId { get; set; }
    public int OriginalCoachId { get; set; }
    public string OriginalCoachCode { get; set; } = string.Empty;
    public string OriginalCoachName { get; set; } = string.Empty;
    public int SubstituteCoachId { get; set; }
    public string SubstituteCoachCode { get; set; } = string.Empty;
    public string SubstituteCoachName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int ActionByUserId { get; set; }
    public DateTime ActionDate { get; set; }
}

public class SubstituteCoachResponseDto
{
    public TrainingSessionDetailDto Session { get; set; } = null!;
    public CoachSubstitutionHistoryDto Substitution { get; set; } = null!;
}

/// <summary>Service-layer result translated to HTTP semantics by the thin controller.</summary>
public class SubstituteCoachActionResult
{
    public SubstituteCoachResponseDto? Data { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public List<ConflictDetail> Conflicts { get; set; } = [];
}
