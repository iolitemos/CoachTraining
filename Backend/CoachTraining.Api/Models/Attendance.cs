using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>
/// Athlete attendance for one Training Session (requirement.md 4.8,
/// FR-RATT-*/FR-PATT-*). Which AttendanceStatus values are allowed per
/// training type is enforced by the Attendance services (todo.md 4.8/4.9).
/// Snapshot fields preserve the athlete's identity at recording time (NFR-002).
/// </summary>
public class Attendance
{
    public int AttendanceId { get; set; }

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;

    public string AthleteCodeSnapshot { get; set; } = string.Empty;
    public string AthleteNameSnapshot { get; set; } = string.Empty;

    public AttendanceStatus Status { get; set; }

    /// <summary>Optional, used for Late status (FR-RATT-006/FR-PATT-003).</summary>
    public TimeOnly? ArrivalTime { get; set; }

    /// <summary>Optional, used for Absent/Excused status (FR-PATT-004).</summary>
    public string? Remark { get; set; }

    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;

    public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
}
