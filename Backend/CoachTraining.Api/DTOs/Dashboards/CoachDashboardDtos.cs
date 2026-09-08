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
    /// <summary>Past sessions that still require the signed-in Coach to start,
    /// complete, or submit the teaching record. Newest sessions are returned first.</summary>
    public List<CoachDashboardSessionDto> OverdueActionSessions { get; set; } = [];
    public List<CoachDashboardSessionDto> TodaySessions { get; set; } = [];
    public List<CoachDashboardSessionDto> UpcomingSessions { get; set; } = [];

    /// <summary>Counts below are scoped to the current calendar month.</summary>
    public int CompletedSessionCount { get; set; }
    public int RemainingSessionCount { get; set; }
    public int RoutineSessionCount { get; set; }
    public int PrivateSessionCount { get; set; }

    /// <summary>Sessions still needing a Coach action: due-or-overdue Scheduled/InProgress
    /// sessions, and Completed sessions not yet submitted (FR-TEACH-006).</summary>
    public int PendingActionCount { get; set; }

    /// <summary>Distinct dates in the month on which this coach is credited with
    /// at least one completed teaching session.</summary>
    public int MonthlyTeachingDayCount { get; set; }
}

/// <summary>Privacy-limited colleague presence for the Coach calendar. Deliberately
/// excludes session identifiers, times, status, and other operational details.</summary>
public class CoachCalendarColleagueDto
{
    public DateOnly SessionDate { get; set; }
    public string CoachNickname { get; set; } = string.Empty;
}
