using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>
/// Recurring Routine Training schedule (requirement.md 4.4, FR-ROUTINE-*).
/// Deliberately has no Group, Team, Location, or fixed athlete roster field —
/// see requirement.md 8.1 and CLAUDE.md 4.2.
/// Editing this schedule only changes future occurrences: each generated
/// TrainingSession (todo.md 4.3) captures its own scheduled time at creation,
/// so completed historical sessions are never altered by later edits here.
/// </summary>
public class RoutineSchedule : AuditableEntity
{
    public int RoutineScheduleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    /// <summary>Free-text recurrence description (e.g. "Weekly"). No fixed value list in requirement.md.</summary>
    public string RecurrencePattern { get; set; } = "Weekly";

    public bool IsActive { get; set; } = true;

    public string? Remarks { get; set; }
}
