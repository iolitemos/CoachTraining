using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Dashboards;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Coach Home dashboard (requirement.md 6.16, todo.md 4.16).</summary>
public class CoachDashboardService : ICoachDashboardService
{
    /// <summary>Statuses still occupying the coach's future calendar (shown as "upcoming").</summary>
    private static readonly SessionStatus[] UpcomingStatuses = [SessionStatus.Scheduled, SessionStatus.CoachAbsent];

    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;

    public CoachDashboardService(ApplicationDbContext db, ISessionStatusService sessionStatusService)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
    }

    public async Task<CoachDashboardResponseDto> GetDashboardAsync(int coachId) => await GetDashboardAsync(coachId, DateTime.Now);

    /// <summary>Overload accepting the current moment explicitly so "today"/"this month"
    /// boundaries are deterministic in tests.</summary>
    public async Task<CoachDashboardResponseDto> GetDashboardAsync(int coachId, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var coachSessions = _db.TrainingSessions
            .Where(s => s.AssignedCoachId == coachId || s.ActualCoachId == coachId);

        var todaySessions = await coachSessions
            .Where(s => s.SessionDate == today)
            .OrderBy(s => s.ScheduledStartDateTime)
            .Select(ToDashboardSession)
            .ToListAsync();

        var upcomingSessions = await coachSessions
            .Where(s => s.SessionDate > today && UpcomingStatuses.Contains(s.Status))
            .OrderBy(s => s.SessionDate)
            .ThenBy(s => s.ScheduledStartDateTime)
            .Take(10)
            .Select(ToDashboardSession)
            .ToListAsync();

        foreach (var session in todaySessions.Concat(upcomingSessions))
        {
            session.RequiredNextAction = BuildNextAction(session.Status);
        }

        var monthSessions = await coachSessions
            .Where(s => s.SessionDate >= monthStart && s.SessionDate <= monthEnd)
            .Select(s => new { s.TrainingType, s.Status, s.ActualStartDateTime, s.ActualEndDateTime })
            .ToListAsync();

        var completedCount = monthSessions.Count(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status));
        var remainingCount = monthSessions.Count(s => UpcomingStatuses.Contains(s.Status) || s.Status == SessionStatus.InProgress);
        var routineCount = monthSessions.Count(s => s.TrainingType == TrainingType.Routine);
        var privateCount = monthSessions.Count(s => s.TrainingType == TrainingType.Private);
        var monthlyHours = monthSessions
            .Where(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status) && s.ActualStartDateTime is not null && s.ActualEndDateTime is not null)
            .Sum(s => (decimal)(s.ActualEndDateTime!.Value - s.ActualStartDateTime!.Value).TotalHours);

        // FR-TEACH-006 — sessions still needing a Coach action: due-or-overdue
        // Scheduled/InProgress sessions, and Completed sessions not yet submitted.
        var pendingActionCount = await coachSessions.CountAsync(s =>
            s.Status == SessionStatus.Completed ||
            ((s.Status == SessionStatus.Scheduled || s.Status == SessionStatus.InProgress) && s.ScheduledStartDateTime <= now));

        return new CoachDashboardResponseDto
        {
            TodaySessions = todaySessions,
            UpcomingSessions = upcomingSessions,
            CompletedSessionCount = completedCount,
            RemainingSessionCount = remainingCount,
            RoutineSessionCount = routineCount,
            PrivateSessionCount = privateCount,
            PendingActionCount = pendingActionCount,
            MonthlyTeachingHours = Math.Round(monthlyHours, 2),
        };
    }

    private static string? BuildNextAction(SessionStatus status) => status switch
    {
        SessionStatus.Scheduled => "เริ่มฝึกซ้อม",
        SessionStatus.InProgress => "บันทึกการเสร็จสิ้นฝึกซ้อม",
        SessionStatus.Completed => "ส่งตรวจ",
        _ => null,
    };

    private static readonly System.Linq.Expressions.Expression<Func<TrainingSession, CoachDashboardSessionDto>> ToDashboardSession = s => new CoachDashboardSessionDto
    {
        TrainingSessionId = s.TrainingSessionId,
        TrainingType = s.TrainingType,
        SessionDate = s.SessionDate,
        ScheduledStartDateTime = s.ScheduledStartDateTime,
        ScheduledEndDateTime = s.ScheduledEndDateTime,
        Status = s.Status,
    };
}
