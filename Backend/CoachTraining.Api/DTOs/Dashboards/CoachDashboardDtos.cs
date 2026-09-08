using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Dashboards;

/// <summary>One session row shown on the Coach Home dashboard (FR-CDASH-001–003).</summary>
public class CoachDashboardSessionDto
{
    public int TrainingSessionId { get; set; }
    public TrainingType TrainingType { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }
    public SessionStatus Status { get; set; }

    /// <summary>Thai label for the action the Coach still needs to take, or null when the
    /// session is finalized / needs no Coach action (FR-CDASH-003).</summary>
    public string? RequiredNextAction { get; set; }
}

/// <summary>Coach Home dashboard response (requirement.md 6.16, todo.md 4.16).</summary>
public class CoachDashboardResponseDto
{
    public List<CoachDashboardSessionDto> TodaySessions { get; set; } = [];
    public List<CoachDashboardSessionDto> UpcomingSessions { get; set; } = [];

    /// <summary>Counts below are scoped to the current calendar month, paired with
    /// the monthly teaching-hour summary.</summary>
    public int CompletedSessionCount { get; set; }
    public int RemainingSessionCount { get; set; }
    public int RoutineSessionCount { get; set; }
    public int PrivateSessionCount { get; set; }

    /// <summary>Sessions still needing a Coach action: due-or-overdue Scheduled/InProgress
    /// sessions, and Completed sessions not yet submitted (FR-TEACH-006).</summary>
    public int PendingActionCount { get; set; }

    public decimal MonthlyTeachingHours { get; set; }
}
