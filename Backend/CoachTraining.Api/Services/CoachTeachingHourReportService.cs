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
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CreditedCoachCode = s.ActualCoachId != null ? s.ActualCoachCodeSnapshot! : s.AssignedCoachCodeSnapshot,
                CreditedCoachName = s.ActualCoachId != null ? s.ActualCoachNameSnapshot! : s.AssignedCoachNameSnapshot,
                CreditedCoachNickname = s.ActualCoachId != null ? s.ActualCoach!.Nickname : s.AssignedCoach.Nickname,
                CreditedCoachColorHex = s.ActualCoachId != null ? s.ActualCoach!.ColorHex : s.AssignedCoach.ColorHex,
            })
            .ToListAsync();

        // FR-RPT-COACH-008/009 — CountsAsCompletedTeaching already excludes
        // Cancelled, Rescheduled(-original), and un-substituted Coach Absent
        // sessions, and only counts sessions that reached Completed or a later
        // finalized status per the approval rules.
        var items = sessions
            .Where(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status))
            .GroupBy(s => new
            {
                s.CreditedCoachId,
                s.CreditedCoachCode,
                s.CreditedCoachName,
                s.CreditedCoachNickname,
                s.CreditedCoachColorHex,
            })
            .Select(g =>
            {
                var routineDays = g.Where(s => s.TrainingType == TrainingType.Routine)
                    .Select(s => s.SessionDate)
                    .Distinct()
                    .Count();
                var privateDays = g.Where(s => s.TrainingType == TrainingType.Private)
                    .Select(s => s.SessionDate)
                    .Distinct()
                    .Count();
                var totalDays = g.Select(s => s.SessionDate).Distinct().Count();

                return new CoachTeachingHourReportItemDto
                {
                    CoachId = g.Key.CreditedCoachId,
                    CoachCode = g.Key.CreditedCoachCode,
                    CoachFullName = g.Key.CreditedCoachName,
                    CoachNickname = g.Key.CreditedCoachNickname,
                    CoachColorHex = g.Key.CreditedCoachColorHex,
                    SessionCount = g.Count(),
                    RoutineDays = routineDays,
                    PrivateDays = privateDays,
                    TotalDays = totalDays,
                };
            })
            .OrderByDescending(i => i.TotalDays)
            .ThenBy(i => i.CoachCode)
            .ToList();

        return new CoachTeachingHourReportResponseDto
        {
            Items = items,
            TotalRoutineDays = items.Sum(i => i.RoutineDays),
            TotalPrivateDays = items.Sum(i => i.PrivateDays),
            GrandTotalDays = items.Sum(i => i.TotalDays),
        };
    }
}
