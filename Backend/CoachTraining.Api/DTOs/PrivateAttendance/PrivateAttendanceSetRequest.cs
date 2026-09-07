using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.PrivateAttendance;

/// <summary>FR-PATT — Private attendance supports all four statuses.</summary>
public class PrivateAttendanceSetRequest
{
    [Required(ErrorMessage = "กรุณาเลือกสถานะการเข้าร่วม")]
    public AttendanceStatus Status { get; set; }

    /// <summary>Optional, used only when Status is Late.</summary>
    public TimeOnly? ArrivalTime { get; set; }

    /// <summary>Optional — typically used for Absent / Leave-Excused.</summary>
    public string? Remark { get; set; }
}
