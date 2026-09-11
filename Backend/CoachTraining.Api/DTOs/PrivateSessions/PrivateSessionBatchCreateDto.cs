using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class PrivateSessionBatchCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกโค้ช")]
    public int CoachId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    [Required] public TimeOnly StartTime { get; set; }
    [Required] public TimeOnly EndTime { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    public string? Remarks { get; set; }
    [MinLength(1, ErrorMessage = "กรุณาเลือกนักกีฬาอย่างน้อยหนึ่งคน")]
    public List<int> AthleteIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        if (EndDate < StartDate)
            yield return new ValidationResult("วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่ม", [nameof(EndDate)]);
        if (EndDate.DayNumber - StartDate.DayNumber > 366)
            yield return new ValidationResult("ช่วงวันที่ต้องไม่เกิน 366 วัน", [nameof(EndDate)]);
        if (DaysOfWeek.Count == 0 || DaysOfWeek.Any(day => !Enum.IsDefined(day)))
            yield return new ValidationResult("กรุณาเลือกรูปแบบวันอย่างน้อย 1 วัน", [nameof(DaysOfWeek)]);
        if (AthleteIds.Count == 0 || AthleteIds.Distinct().Count() != AthleteIds.Count)
            yield return new ValidationResult("กรุณาเลือกนักกีฬาโดยไม่ให้มีรายการซ้ำ", [nameof(AthleteIds)]);
    }
}

public class PrivateSessionBatchCreateResult
{
    public int CreatedCount { get; set; }
    public List<DateOnly> CreatedDates { get; set; } = [];
}
