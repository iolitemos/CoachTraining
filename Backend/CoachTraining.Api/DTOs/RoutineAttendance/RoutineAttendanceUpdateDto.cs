using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.RoutineAttendance;

public class RoutineAttendanceUpdateDto : IValidatableObject
{
    [Required(ErrorMessage = "กรุณาเลือกสถานะการเข้าร่วม")]
    public AttendanceStatus Status { get; set; }

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
