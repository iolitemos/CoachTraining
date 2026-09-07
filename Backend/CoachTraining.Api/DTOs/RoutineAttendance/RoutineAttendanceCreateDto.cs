using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.RoutineAttendance;

/// <summary>FR-RATT-005/006 — Routine attendance only supports Present and Late.</summary>
public class RoutineAttendanceCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกนักกีฬา")]
    public int AthleteId { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกสถานะการเข้าร่วม")]
    public AttendanceStatus Status { get; set; }

    /// <summary>Optional, used only when Status is Late.</summary>
    public TimeOnly? ArrivalTime { get; set; }

    public string? Remark { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not (AttendanceStatus.Present or AttendanceStatus.Late))
        {
            yield return new ValidationResult("การฝึกซ้อมประจำอนุญาตเฉพาะสถานะ มาเรียน หรือ มาสาย เท่านั้น", [nameof(Status)]);
        }
    }
}
