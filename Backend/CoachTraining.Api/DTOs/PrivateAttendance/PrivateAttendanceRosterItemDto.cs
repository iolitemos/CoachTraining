using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.PrivateAttendance;

/// <summary>
/// One assigned athlete's attendance state. Status is null when the Coach hasn't
/// recorded it yet — the roster itself always lists every assigned athlete
/// (FR-PATT-001), so a null Status is how "missing" is identified (requirement.md 9.4).
/// </summary>
public class PrivateAttendanceRosterItemDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public int? AttendanceId { get; set; }
    public AttendanceStatus? Status { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public string? Remark { get; set; }
    public DateTime? RecordedDate { get; set; }
}

public class PrivateAttendanceRosterResult
{
    public List<PrivateAttendanceRosterItemDto> Athletes { get; set; } = [];

    /// <summary>True once every assigned athlete has a recorded Status (FR-PATT-002).</summary>
    public bool IsComplete { get; set; }
}
