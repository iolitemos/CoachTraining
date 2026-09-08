using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class RoutineScheduleCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกโค้ช")]
    public int CoachId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันที่ฝึกซ้อม")]
    public DateOnly EffectiveStartDate { get; set; }

    public string? Remarks { get; set; }

    /// <summary>FR-CONFLICT-004 — Administrator confirms proceeding despite a detected coach conflict.</summary>
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
