using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.RoutineAttendance;

public class RoutineAttendanceListItemDto
{
    public int AttendanceId { get; set; }
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public string? Remark { get; set; }
    public DateTime RecordedDate { get; set; }
}
