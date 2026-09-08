using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.RoutineSchedules;

/// <summary>Coach self-service request. CoachId and conflict override are intentionally
/// omitted because the API derives ownership from the authenticated account.</summary>
public class CoachRoutineScheduleCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันที่ฝึกซ้อม")]
    public DateOnly EffectiveStartDate { get; set; }

    public string? Remarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        }
    }
}
