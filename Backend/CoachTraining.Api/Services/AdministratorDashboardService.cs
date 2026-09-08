using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Dashboards;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Administrator Dashboard (requirement.md 6.17, todo.md 4.17).</summary>
public class AdministratorDashboardService : IAdministratorDashboardService
{
    private static readonly SessionStatus[] UpcomingStatuses = [SessionStatus.Scheduled, SessionStatus.CoachAbsent];

    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;

    public AdministratorDashboardService(ApplicationDbContext db, ISessionStatusService sessionStatusService)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
    }

    public Task<AdministratorDashboardResponseDto> GetDashboardAsync(AdministratorDashboardFilterRequest filter) =>
        GetDashboardAsync(filter, DateTime.Now);

    /// <summary>Overload accepting the current moment explicitly so "today" is
    /// deterministic in tests.</summary>
    public async Task<AdministratorDashboardResponseDto> GetDashboardAsync(AdministratorDashboardFilterRequest filter, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var effectiveStart = filter.StartDate ?? today;
        var effectiveEnd = filter.EndDate ?? today;

        var sessionsTodayCount = await ApplyCommonFilters(_db.TrainingSessions.Where(s => s.SessionDate == today), filter).CountAsync();

        var upcomingCount = await ApplyCommonFilters(
            _db.TrainingSessions.Where(s => s.SessionDate >= today && s.SessionDate <= effectiveEnd && UpcomingStatuses.Contains(s.Status)),
            filter).CountAsync();

        var rangeSessions = await ApplyCommonFilters(
                _db.TrainingSessions.Where(s => s.SessionDate >= effectiveStart && s.SessionDate <= effectiveEnd), filter)
            .Select(s => new
            {
                s.TrainingType,
                s.Status,
                s.ActualStartDateTime,
                s.ActualEndDateTime,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CreditedCoachCode = s.ActualCoachId != null ? s.ActualCoachCodeSnapshot! : s.AssignedCoachCodeSnapshot,
                CreditedCoachName = s.ActualCoachId != null ? s.ActualCoachNameSnapshot! : s.AssignedCoachNameSnapshot,
            })
            .ToListAsync();

        var completedCount = rangeSessions.Count(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status));
        var cancelledCount = rangeSessions.Count(s => s.Status == SessionStatus.Cancelled);
        var routineCount = rangeSessions.Count(s => s.TrainingType == TrainingType.Routine);
        var privateCount = rangeSessions.Count(s => s.TrainingType == TrainingType.Private);

        var coachTeachingHours = rangeSessions
            .Where(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status) && s.ActualStartDateTime is not null && s.ActualEndDateTime is not null)
            .GroupBy(s => new { s.CreditedCoachId, s.CreditedCoachCode, s.CreditedCoachName })
            .Select(g => new CoachTeachingHoursDto
            {
                CoachId = g.Key.CreditedCoachId,
                CoachCode = g.Key.CreditedCoachCode,
                CoachFullName = g.Key.CreditedCoachName,
                RoutineHours = Math.Round((decimal)g.Where(s => s.TrainingType == TrainingType.Routine)
                    .Sum(s => (s.ActualEndDateTime!.Value - s.ActualStartDateTime!.Value).TotalHours), 2),
                PrivateHours = Math.Round((decimal)g.Where(s => s.TrainingType == TrainingType.Private)
                    .Sum(s => (s.ActualEndDateTime!.Value - s.ActualStartDateTime!.Value).TotalHours), 2),
            })
            .OrderByDescending(c => c.RoutineHours + c.PrivateHours)
            .ToList();

        foreach (var coach in coachTeachingHours)
        {
            coach.TotalHours = coach.RoutineHours + coach.PrivateHours;
        }

        var coachesTeachingToday = await ApplyCommonFilters(_db.TrainingSessions.Where(s => s.SessionDate == today), filter)
            .Select(s => new
            {
                s.TrainingSessionId,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CreditedCoachCode = s.ActualCoachId != null ? s.ActualCoachCodeSnapshot! : s.AssignedCoachCodeSnapshot,
                CreditedCoachName = s.ActualCoachId != null ? s.ActualCoachNameSnapshot! : s.AssignedCoachNameSnapshot,
            })
            .ToListAsync();

        var coachesTeachingTodayDto = coachesTeachingToday
            .GroupBy(s => new { s.CreditedCoachId, s.CreditedCoachCode, s.CreditedCoachName })
            .Select(g => new CoachTeachingTodayDto
            {
                CoachId = g.Key.CreditedCoachId,
                CoachCode = g.Key.CreditedCoachCode,
                CoachFullName = g.Key.CreditedCoachName,
                SessionCount = g.Count(),
                TrainingSessionIds = g.Select(s => s.TrainingSessionId).ToList(),
            })
            .OrderBy(c => c.CoachFullName)
            .ToList();

        var attendanceQuery = _db.Attendances
            .Where(a => a.TrainingSession.SessionDate >= effectiveStart && a.TrainingSession.SessionDate <= effectiveEnd)
            .AsQueryable();
        if (filter.CoachId is not null)
        {
            attendanceQuery = attendanceQuery.Where(a =>
                a.TrainingSession.AssignedCoachId == filter.CoachId || a.TrainingSession.ActualCoachId == filter.CoachId);
        }

        if (filter.TrainingType is not null)
        {
            attendanceQuery = attendanceQuery.Where(a => a.TrainingSession.TrainingType == filter.TrainingType);
        }

        var attendanceStatuses = await attendanceQuery.Select(a => a.Status).ToListAsync();
        var attendanceSummary = new AttendanceSummaryDto
        {
            PresentCount = attendanceStatuses.Count(s => s == AttendanceStatus.Present),
            AbsentCount = attendanceStatuses.Count(s => s == AttendanceStatus.Absent),
            LateCount = attendanceStatuses.Count(s => s == AttendanceStatus.Late),
            ExcusedCount = attendanceStatuses.Count(s => s == AttendanceStatus.Excused),
        };

        return new AdministratorDashboardResponseDto
        {
            SessionsTodayCount = sessionsTodayCount,
            CompletedCount = completedCount,
            UpcomingCount = upcomingCount,
            CancelledCount = cancelledCount,
            RoutineCount = routineCount,
            PrivateCount = privateCount,
            CoachesTeachingToday = coachesTeachingTodayDto,
            AttendanceSummary = attendanceSummary,
            CoachTeachingHours = coachTeachingHours,
        };
    }

    private static IQueryable<TrainingSession> ApplyCommonFilters(IQueryable<TrainingSession> query, AdministratorDashboardFilterRequest filter)
    {
        if (filter.CoachId is not null)
        {
            query = query.Where(s => s.AssignedCoachId == filter.CoachId || s.ActualCoachId == filter.CoachId);
        }

        if (filter.TrainingType is not null)
        {
            query = query.Where(s => s.TrainingType == filter.TrainingType);
        }

        return query;
    }
}
