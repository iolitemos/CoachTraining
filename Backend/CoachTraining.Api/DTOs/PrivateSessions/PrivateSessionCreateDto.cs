using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class PrivateSessionCreateDto : IValidatableObject
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

    public List<int> AthleteIds { get; set; } = [];
    public List<GuestParticipantDto> GuestParticipants { get; set; } = [];

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

        if (AthleteIds.Distinct().Count() != AthleteIds.Count)
        {
            yield return new ValidationResult("พบนักกีฬาซ้ำในรายการที่เลือก", [nameof(AthleteIds)]);
        }

        if (AthleteIds.Count + GuestParticipants.Count == 0)
            yield return new ValidationResult("กรุณาเพิ่มผู้เข้าร่วมอย่างน้อยหนึ่งคน", [nameof(AthleteIds), nameof(GuestParticipants)]);
        if (GuestParticipants.Any(g => string.IsNullOrWhiteSpace(g.FullName)))
            yield return new ValidationResult("กรุณากรอกชื่อผู้เรียนชั่วคราว", [nameof(GuestParticipants)]);

        if (OverrideConflict && string.IsNullOrWhiteSpace(OverrideReason))
        {
            yield return new ValidationResult("กรุณาระบุเหตุผลในการยืนยันดำเนินการทั้งที่มีตารางทับซ้อน", [nameof(OverrideReason)]);
        }
    }
}
