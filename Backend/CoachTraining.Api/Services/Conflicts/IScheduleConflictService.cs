using CoachTraining.Api.DTOs.Conflicts;

namespace CoachTraining.Api.Services;

/// <summary>
/// Centralized schedule-conflict detection (requirement.md FR-CONFLICT-001–005,
/// todo.md 4.14) reused by Routine (4.3), Private (4.4), Substitute Coach (4.11),
/// and Rescheduling (4.13).
/// </summary>
public interface IScheduleConflictService
{
    /// <summary>
    /// Finds existing Training Sessions for the given coach whose scheduled time
    /// overlaps [startDateTime, endDateTime), across both Routine and Private
    /// Training (FR-CONFLICT-001, FR-CONFLICT-003 — a coach's own Routine Training
    /// is just another Training Session). Cancelled and Rescheduled-original
    /// sessions never occupy a slot, so they are excluded (FR-SESSION-009, FR-CR-006).
    /// </summary>
    Task<List<ConflictDetail>> CheckCoachOverlapAsync(
        int coachId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeTrainingSessionId = null);

    /// <summary>
    /// Finds, for each given athlete, existing Private Training assignments whose
    /// session time overlaps [startDateTime, endDateTime) — FR-CONFLICT-002.
    /// Routine Training has no fixed athlete roster, so it never contributes here.
    /// </summary>
    Task<List<ConflictDetail>> CheckAthleteOverlapAsync(
        IReadOnlyCollection<int> athleteIds,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeTrainingSessionId = null);

    /// <summary>
    /// Finds other active Routine Training schedules for the same coach whose
    /// day-of-week, time-of-day, and effective date range overlap this one
    /// (FR-ROUTINE-008) — a template-level check, independent of any generated sessions.
    /// </summary>
    Task<List<RoutineTemplateConflictDetail>> CheckRoutineTemplateOverlapAsync(
        int coachId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly effectiveStartDate,
        DateOnly? effectiveEndDate,
        int? excludeRoutineScheduleId = null);
}
