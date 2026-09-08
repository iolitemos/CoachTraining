namespace CoachTraining.Api.DTOs.Conflicts;

/// <summary>A conflict between two Routine Training schedules for the same coach,
/// selected date, and overlapping time.</summary>
public class RoutineTemplateConflictDetail
{
    public int ConflictingRoutineScheduleId { get; set; }
    public string ConflictingRoutineScheduleName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
