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
                s.SessionDate,
                s.TrainingType,
                s.Status,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CoachNickname = s.ActualCoachId != null
                    ? (s.ActualCoach!.Nickname ?? s.ActualCoach.FullName)
                    : (s.AssignedCoach.Nickname ?? s.AssignedCoach.FullName),
                CoachColorHex = s.ActualCoachId != null ? s.ActualCoach!.ColorHex : s.AssignedCoach.ColorHex,
            })
            .ToListAsync();

        var completedCount = rangeSessions.Count(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status));
        var cancelledCount = rangeSessions.Count(s => s.Status == SessionStatus.Cancelled);
        var routineCount = rangeSessions.Count(s => s.TrainingType == TrainingType.Routine);
        var privateCount = rangeSessions.Count(s => s.TrainingType == TrainingType.Private);

        var coachesTeachingToday = await ApplyCommonFilters(_db.TrainingSessions.Where(s => s.SessionDate == today), filter)
            .Select(s => new
            {
                s.TrainingSessionId,
                s.TrainingType,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CoachNickname = s.ActualCoachId != null
                    ? (s.ActualCoach!.Nickname ?? s.ActualCoach.FullName)
                    : (s.AssignedCoach.Nickname ?? s.AssignedCoach.FullName),
                CoachColorHex = s.ActualCoachId != null ? s.ActualCoach!.ColorHex : s.AssignedCoach.ColorHex,
            })
            .ToListAsync();

        var coachesTeachingTodayDto = coachesTeachingToday
            .GroupBy(s => new { s.CreditedCoachId, s.CoachNickname, s.CoachColorHex, s.TrainingType })
            .Select(g => new CoachTeachingTodayDto
            {
                CoachId = g.Key.CreditedCoachId,
                CoachNickname = g.Key.CoachNickname,
                CoachColorHex = g.Key.CoachColorHex,
                TrainingType = g.Key.TrainingType,
                SessionCount = g.Count(),
                TrainingSessionIds = g.Select(s => s.TrainingSessionId).ToList(),
            })
            .OrderBy(c => c.TrainingType)
            .ThenBy(c => c.CoachNickname)
            .ToList();

        var attendanceQuery = _db.Attendances
            .Where(a => a.TrainingSession.SessionDate >= effectiveStart && a.TrainingSession.SessionDate <= effectiveEnd)
            .Where(a => a.TrainingSession.Status != SessionStatus.Rescheduled)
            .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
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

        var attendanceRecords = await attendanceQuery
            .Select(a => new
            {
                AthleteId = a.AthleteId ?? -a.PrivateSessionAthleteId!.Value,
                AthleteName = a.AthleteId.HasValue ? (a.Athlete!.Nickname ?? a.Athlete.FullName) : a.AthleteNameSnapshot,
                a.TrainingSession.TrainingType,
                Date = a.TrainingSession.SessionDate,
            })
            .ToListAsync();

        static AttendanceByTrainingTypeDto SummarizeAttendance(
            DateOnly startDate,
            DateOnly endDate,
            IEnumerable<(int AthleteId, string AthleteName, DateOnly Date)> records,
            IEnumerable<(DateOnly Date, int CoachId, string CoachNickname, string CoachColorHex)> sessions)
        {
            var recordList = records.ToList();
            var sessionList = sessions.ToList();
            var dayCount = Math.Max(0, endDate.DayNumber - startDate.DayNumber + 1);
            var athleteGroups = recordList
                .GroupBy(a => a.AthleteName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new
                {
                    AthleteId = g.Min(a => a.AthleteId),
                    AthleteName = g.Key,
                    Records = g.ToList(),
                })
                .ToList();

            return new AttendanceByTrainingTypeDto
            {
                TotalAttendance = recordList.Count,
                Athletes = athleteGroups
                    .Select(g => new AthleteAttendanceSummaryItemDto
                    {
                        AthleteId = g.AthleteId,
                        AthleteName = g.AthleteName,
                        AttendanceCount = g.Records.Count,
                    })
                    .OrderBy(a => a.AthleteName)
                    .ToList(),
                DailySummaries = Enumerable.Range(0, dayCount)
                    .Select(dayOffset => startDate.AddDays(dayOffset))
                    .Select(date => new DailyAttendanceSummaryDto
                    {
                        Date = date,
                        TotalAttendance = recordList.Count(a => a.Date == date),
                        Attendances = athleteGroups
                            .Select(g => new DailyAttendanceCellDto
                            {
                                AthleteId = g.AthleteId,
                                AttendanceCount = g.Records.Count(a => a.Date == date),
                            })
                            .Where(a => a.AttendanceCount > 0)
                            .ToList(),
                        Coaches = sessionList
                            .Where(s => s.Date == date)
                            .GroupBy(s => new { s.CoachId, s.CoachNickname, s.CoachColorHex })
                            .Select(g => new DailyAttendanceCoachDto
                            {
                                CoachId = g.Key.CoachId,
                                CoachNickname = g.Key.CoachNickname,
                                CoachColorHex = g.Key.CoachColorHex,
                            })
                            .OrderBy(c => c.CoachNickname)
                            .ToList(),
                    })
                    .ToList(),
            };
        }

        var attendanceSummary = new AttendanceSummaryDto
        {
            Routine = SummarizeAttendance(
                effectiveStart,
                effectiveEnd,
                attendanceRecords
                .Where(a => a.TrainingType == TrainingType.Routine)
                .Select(a => (a.AthleteId, a.AthleteName, a.Date)),
                rangeSessions
                    .Where(s => s.TrainingType == TrainingType.Routine && s.Status != SessionStatus.Cancelled && s.Status != SessionStatus.Rescheduled)
                    .Select(s => (s.SessionDate, s.CreditedCoachId, s.CoachNickname, s.CoachColorHex))),
            Private = SummarizeAttendance(
                effectiveStart,
                effectiveEnd,
                attendanceRecords
                .Where(a => a.TrainingType == TrainingType.Private)
                .Select(a => (a.AthleteId, a.AthleteName, a.Date)),
                rangeSessions
                    .Where(s => s.TrainingType == TrainingType.Private && s.Status != SessionStatus.Cancelled && s.Status != SessionStatus.Rescheduled)
                    .Select(s => (s.SessionDate, s.CreditedCoachId, s.CoachNickname, s.CoachColorHex))),
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
