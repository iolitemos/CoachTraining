using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Coach Teaching-Hour Report (requirement.md 6.18, todo.md 4.18).</summary>
public class CoachTeachingHourReportService : ICoachTeachingHourReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;

    public CoachTeachingHourReportService(ApplicationDbContext db, ISessionStatusService sessionStatusService)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
    }

    public async Task<CoachTeachingHourReportResponseDto> GetReportAsync(CoachTeachingHourReportFilter filter)
    {
        var query = _db.TrainingSessions.AsQueryable();

        if (filter.StartDate is not null)
        {
            query = query.Where(s => s.SessionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate is not null)
        {
            query = query.Where(s => s.SessionDate <= filter.EndDate.Value);
        }

        if (filter.TrainingType is not null)
        {
            query = query.Where(s => s.TrainingType == filter.TrainingType.Value);
        }

        if (filter.CoachId is not null)
        {
            // FR-RPT-COACH-007 — filter by whoever is credited (actual coach after
            // a substitution), not only the originally assigned coach.
            query = query.Where(s => s.AssignedCoachId == filter.CoachId || s.ActualCoachId == filter.CoachId);
        }

        var sessions = await query
            .Select(s => new
            {
                s.TrainingType,
                s.SessionDate,
                s.Status,
                s.AssignedCoachId,
                AssignedCoachCode = s.AssignedCoachCodeSnapshot,
                AssignedCoachName = s.AssignedCoachNameSnapshot,
                AssignedCoachNickname = s.AssignedCoach.Nickname,
                AssignedCoachColorHex = s.AssignedCoach.ColorHex,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CreditedCoachCode = s.ActualCoachId != null ? s.ActualCoachCodeSnapshot! : s.AssignedCoachCodeSnapshot,
                CreditedCoachName = s.ActualCoachId != null ? s.ActualCoachNameSnapshot! : s.AssignedCoachNameSnapshot,
                CreditedCoachNickname = s.ActualCoachId != null ? s.ActualCoach!.Nickname : s.AssignedCoach.Nickname,
                CreditedCoachColorHex = s.ActualCoachId != null ? s.ActualCoach!.ColorHex : s.AssignedCoach.ColorHex,
            })
            .ToListAsync();

        // Plan belongs to the originally assigned coach and excludes sessions that
        // are no longer on the active schedule. Actual belongs to the coach who
        // really taught and follows the centralized finalized-teaching rule.
        var plannedSessions = sessions
            .Where(s => s.Status is not SessionStatus.Cancelled and not SessionStatus.Rescheduled)
            .ToList();
        var actualSessions = sessions
            .Where(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status))
            .ToList();
        var coachIds = plannedSessions.Select(s => s.AssignedCoachId)
            .Concat(actualSessions.Select(s => s.CreditedCoachId))
            .Where(coachId => filter.CoachId is null || coachId == filter.CoachId.Value)
            .Distinct();

        var items = coachIds.Select(coachId =>
            {
                var planned = plannedSessions.Where(s => s.AssignedCoachId == coachId).ToList();
                var actual = actualSessions.Where(s => s.CreditedCoachId == coachId).ToList();
                var identity = actual.FirstOrDefault();
                var plannedIdentity = planned.FirstOrDefault();
                var routineDays = actual.Where(s => s.TrainingType == TrainingType.Routine).Select(s => s.SessionDate).Distinct().Count();
                var privateDays = actual.Where(s => s.TrainingType == TrainingType.Private).Select(s => s.SessionDate).Distinct().Count();

                return new CoachTeachingHourReportItemDto
                {
                    CoachId = coachId,
                    CoachCode = identity?.CreditedCoachCode ?? plannedIdentity!.AssignedCoachCode,
                    CoachFullName = identity?.CreditedCoachName ?? plannedIdentity!.AssignedCoachName,
                    CoachNickname = identity?.CreditedCoachNickname ?? plannedIdentity?.AssignedCoachNickname,
                    CoachColorHex = identity?.CreditedCoachColorHex ?? plannedIdentity?.AssignedCoachColorHex ?? string.Empty,
                    SessionCount = actual.Count,
                    PlannedSessionCount = planned.Count,
                    ActualSessionCount = actual.Count,
                    PlannedDays = planned.Select(s => s.SessionDate).Distinct().Count(),
                    ActualDays = actual.Select(s => s.SessionDate).Distinct().Count(),
                    RoutineDays = routineDays,
                    PrivateDays = privateDays,
                    TotalDays = actual.Select(s => s.SessionDate).Distinct().Count(),
                };
            })
            .OrderByDescending(i => i.ActualDays)
            .ThenByDescending(i => i.PlannedDays)
            .ThenBy(i => i.CoachCode)
            .ToList();

        return new CoachTeachingHourReportResponseDto
        {
            Items = items,
            TotalRoutineDays = items.Sum(i => i.RoutineDays),
            TotalPrivateDays = items.Sum(i => i.PrivateDays),
            GrandTotalDays = items.Sum(i => i.TotalDays),
            TotalPlannedDays = items.Sum(i => i.PlannedDays),
            TotalActualDays = items.Sum(i => i.ActualDays),
        };
    }
}
