using CoachTraining.Api.Models.Common;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>
/// Central operational record for both Routine and Private Training
/// (requirement.md 4.6, FR-SESSION-*). Assigned Coach and Actual Coach are
/// always separate concepts (CLAUDE.md 4.4); coach identity is snapshotted at
/// the time of the session so later master-data changes never rewrite history
/// (NFR-002).
/// </summary>
public class TrainingSession : AuditableEntity
{
    public int TrainingSessionId { get; set; }

    public TrainingType TrainingType { get; set; }

    /// <summary>Set only for sessions generated from a Routine Training schedule.</summary>
    public int? RoutineScheduleId { get; set; }
    public RoutineSchedule? RoutineSchedule { get; set; }

    public DateOnly SessionDate { get; set; }

    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }

    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }

    /// <summary>Originally assigned coach — never overwritten by a substitution (FR-SESSION-004).</summary>
    public int AssignedCoachId { get; set; }
    public Coach AssignedCoach { get; set; } = null!;
    public string AssignedCoachCodeSnapshot { get; set; } = string.Empty;
    public string AssignedCoachNameSnapshot { get; set; } = string.Empty;

    /// <summary>Coach who actually taught the session — credited for teaching hours (FR-SESSION-005).</summary>
    public int? ActualCoachId { get; set; }
    public Coach? ActualCoach { get; set; }
    public string? ActualCoachCodeSnapshot { get; set; }
    public string? ActualCoachNameSnapshot { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    /// <summary>Optional, Private Training only (requirement.md 4.5 / FR-PRIVATE-006).</summary>
    public string? Location { get; set; }

    public string? Remarks { get; set; }

    public string? CancellationReason { get; set; }

    /// <summary>Set on the replacement session, pointing back to the original (FR-SESSION-010, FR-CR-006).</summary>
    public int? OriginalSessionId { get; set; }
    public TrainingSession? OriginalSession { get; set; }
    public ICollection<TrainingSession> ReplacementSessions { get; set; } = [];

    public bool IsConflictOverridden { get; set; }
    public string? ConflictOverrideReason { get; set; }

    public ICollection<PrivateSessionAthlete> PrivateAthletes { get; set; } = [];
    public ICollection<Attendance> Attendances { get; set; } = [];
    public TrainingLog? TrainingLog { get; set; }
}
