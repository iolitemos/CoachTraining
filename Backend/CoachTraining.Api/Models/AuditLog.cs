namespace CoachTraining.Api.Models;

/// <summary>
/// General-purpose audit trail for material business changes (requirement.md
/// FR-AUDIT-001–003) that aren't already covered by a dedicated history table
/// (approval, substitution, conflict-override).
/// </summary>
public class AuditLog
{
    public int AuditLogId { get; set; }

    /// <summary>e.g. "Coach", "RoutineSchedule", "TrainingSession".</summary>
    public string EntityName { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>e.g. "Create", "Update", "Deactivate".</summary>
    public string Action { get; set; } = string.Empty;

    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }

    public int ActionByUserId { get; set; }
    public User ActionByUser { get; set; } = null!;

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
