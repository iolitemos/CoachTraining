namespace CoachTraining.Api.DTOs.Conflicts;

/// <summary>A conflict between two recurring Routine Training schedules (same coach,
/// overlapping day-of-week/time/effective-date-range) — FR-ROUTINE-008.</summary>
public class RoutineTemplateConflictDetail
{
    public int ConflictingRoutineScheduleId { get; set; }
    public string ConflictingRoutineScheduleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
