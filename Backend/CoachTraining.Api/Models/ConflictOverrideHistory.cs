using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>Authorized schedule-conflict override audit trail (requirement.md FR-CONFLICT-004/005).</summary>
public class ConflictOverrideHistory
{
    public int ConflictOverrideHistoryId { get; set; }

    public ConflictType ConflictType { get; set; }

    /// <summary>The affected session, when the override was applied to a Training Session.</summary>
    public int? TrainingSessionId { get; set; }
    public TrainingSession? TrainingSession { get; set; }

    /// <summary>The affected schedule, when the override was applied to a Routine Schedule.</summary>
    public int? RoutineScheduleId { get; set; }
    public RoutineSchedule? RoutineSchedule { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int ActionByUserId { get; set; }
    public User ActionByUser { get; set; } = null!;

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
