using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>
/// Single-date Routine Training schedule.
/// Deliberately has no Group, Team, Location, or fixed athlete roster field —
/// see requirement.md 8.1 and CLAUDE.md 4.2.
/// Each generated TrainingSession captures its own scheduled time at creation,
/// so completed historical sessions are never altered by later edits here.
/// </summary>
public class RoutineSchedule : AuditableEntity
{
    public int RoutineScheduleId { get; set; }

    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remarks { get; set; }
}
