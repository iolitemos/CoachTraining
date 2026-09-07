namespace CoachTraining.Api.DTOs.PrivateAttendance;

public class PrivateAttendanceActionResult
{
    public PrivateAttendanceRosterItemDto? Attendance { get; set; }

    /// <summary>Whether every assigned athlete now has a recorded status, after this action.</summary>
    public bool RosterComplete { get; set; }

    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
}
