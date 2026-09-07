using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.RoutineSchedules;

/// <summary>
/// Updates only apply to the recurring schedule configuration itself — already
/// generated TrainingSession rows (past or future) keep the scheduled time they
/// were created with (FR-ROUTINE-007); new generation runs use the updated values.
/// </summary>
public class RoutineScheduleUpdateDto : IValidatableObject
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกโค้ช")]
    public int CoachId { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันในสัปดาห์")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันที่เริ่มมีผล")]
    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    [MaxLength(50)]
    public string? RecurrencePattern { get; set; }

    public string? Remarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        }

        if (EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult("วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มมีผล", [nameof(EffectiveEndDate)]);
        }
    }
}

public class RoutineScheduleStatusUpdateDto
{
    public bool IsActive { get; set; }
}
