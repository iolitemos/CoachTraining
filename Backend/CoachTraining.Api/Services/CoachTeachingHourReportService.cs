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
                s.Status,
                s.ActualStartDateTime,
                s.ActualEndDateTime,
                CreditedCoachId = s.ActualCoachId ?? s.AssignedCoachId,
                CreditedCoachCode = s.ActualCoachId != null ? s.ActualCoachCodeSnapshot! : s.AssignedCoachCodeSnapshot,
                CreditedCoachName = s.ActualCoachId != null ? s.ActualCoachNameSnapshot! : s.AssignedCoachNameSnapshot,
            })
            .ToListAsync();

        // FR-RPT-COACH-008/009 — CountsAsCompletedTeaching already excludes
        // Cancelled, Rescheduled(-original), and un-substituted Coach Absent
        // sessions, and only counts sessions that reached Completed or a later
        // finalized status per the approval rules.
        var items = sessions
            .Where(s => _sessionStatusService.CountsAsCompletedTeaching(s.Status) && s.ActualStartDateTime is not null && s.ActualEndDateTime is not null)
            .GroupBy(s => new { s.CreditedCoachId, s.CreditedCoachCode, s.CreditedCoachName })
            .Select(g =>
            {
                var routineHours = Math.Round((decimal)g.Where(s => s.TrainingType == TrainingType.Routine)
                    .Sum(s => (s.ActualEndDateTime!.Value - s.ActualStartDateTime!.Value).TotalHours), 2);
                var privateHours = Math.Round((decimal)g.Where(s => s.TrainingType == TrainingType.Private)
                    .Sum(s => (s.ActualEndDateTime!.Value - s.ActualStartDateTime!.Value).TotalHours), 2);

                return new CoachTeachingHourReportItemDto
                {
                    CoachId = g.Key.CreditedCoachId,
                    CoachCode = g.Key.CreditedCoachCode,
                    CoachFullName = g.Key.CreditedCoachName,
                    SessionCount = g.Count(),
                    RoutineHours = routineHours,
                    PrivateHours = privateHours,
                    TotalHours = routineHours + privateHours,
                };
            })
            .OrderByDescending(i => i.TotalHours)
            .ToList();

        return new CoachTeachingHourReportResponseDto
        {
            Items = items,
            TotalRoutineHours = items.Sum(i => i.RoutineHours),
            TotalPrivateHours = items.Sum(i => i.PrivateHours),
            GrandTotalHours = items.Sum(i => i.TotalHours),
        };
    }
}
