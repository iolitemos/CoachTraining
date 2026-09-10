using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class CoachRoutineScheduleBatchCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public string? Remarks { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        }

        if (EndDate < StartDate)
        {
            yield return new ValidationResult("วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่ม", [nameof(EndDate)]);
        }

        if (EndDate.DayNumber - StartDate.DayNumber > 366)
        {
            yield return new ValidationResult("ช่วงวันที่ต้องไม่เกิน 366 วัน", [nameof(EndDate)]);
        }

        if (DaysOfWeek.Count == 0 || DaysOfWeek.Any(day => !Enum.IsDefined(day)))
        {
            yield return new ValidationResult("กรุณาเลือกวันในสัปดาห์อย่างน้อย 1 วัน", [nameof(DaysOfWeek)]);
        }
    }
}

public class CoachRoutineScheduleBatchCreateResult
{
    public int CreatedCount { get; set; }
    public List<DateOnly> CreatedDates { get; set; } = [];
}
