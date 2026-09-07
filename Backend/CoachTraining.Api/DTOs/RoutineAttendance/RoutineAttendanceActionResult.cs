namespace CoachTraining.Api.DTOs.RoutineAttendance;

public class RoutineAttendanceActionResult
{
    public RoutineAttendanceListItemDto? Attendance { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public bool Success { get; set; }
}
