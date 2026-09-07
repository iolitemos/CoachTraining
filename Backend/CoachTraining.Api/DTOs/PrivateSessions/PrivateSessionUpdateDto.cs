using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.PrivateSessions;

/// <summary>Only sessions still in the Scheduled status may be updated (FR-PRIVATE-009).</summary>
public class PrivateSessionUpdateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกโค้ช")]
    public int CoachId { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกวันที่ฝึกซ้อม")]
    public DateOnly SessionDate { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาเริ่ม")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเวลาสิ้นสุด")]
    public TimeOnly EndTime { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public string? Remarks { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกนักกีฬาอย่างน้อยหนึ่งคน")]
    [MinLength(1, ErrorMessage = "กรุณาเลือกนักกีฬาอย่างน้อยหนึ่งคน")]
    public List<int> AthleteIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่ม", [nameof(EndTime)]);
        }

        if (AthleteIds.Distinct().Count() != AthleteIds.Count)
        {
            yield return new ValidationResult("พบนักกีฬาซ้ำในรายการที่เลือก", [nameof(AthleteIds)]);
        }
    }
}
