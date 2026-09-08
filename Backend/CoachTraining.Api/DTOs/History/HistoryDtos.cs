using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.History;

/// <summary>General-purpose audit trail entry (FR-AUDIT-001–003) — schedule
/// creation/change, cancellation, and rescheduling events land here.</summary>
public class AuditLogEntryDto
{
    public int AuditLogId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public int ActionByUserId { get; set; }
    public DateTime ActionDate { get; set; }
}

/// <summary>One recorded schedule-conflict override (FR-CONFLICT-005).</summary>
public class ConflictOverrideHistoryEntryDto
{
    public int ConflictOverrideHistoryId { get; set; }
    public ConflictType ConflictType { get; set; }
    public int? TrainingSessionId { get; set; }
    public int? RoutineScheduleId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ActionByUserId { get; set; }
    public DateTime ActionDate { get; set; }
}

/// <summary>Service-layer result translated to HTTP semantics by the thin controller.</summary>
public class HistoryAccessResult<T>
{
    public T? Data { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
}
