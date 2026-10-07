using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Athlete Attendance Report (requirement.md 6.19, todo.md 4.19).</summary>
public class AthleteAttendanceReportService : IAthleteAttendanceReportService
{
    public AthleteAttendanceReportService(ApplicationDbContext db)
    {
        _db = db;
    }

    private readonly ApplicationDbContext _db;

    public async Task<AthleteAttendanceReportResponseDto> GetReportAsync(AthleteAttendanceReportFilter filter)
    {
        var query = _db.Attendances
            // FR-RPT-ATH-007 — a Rescheduled-original session never happened at its
            // original time; its attendance (if any was recorded before the move)
            // must not double-count against the replacement session.
            .Where(a => a.TrainingSession.Status != SessionStatus.Rescheduled)
            .Where(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late)
            .AsQueryable();

        if (filter.AthleteId is not null)
        {
            query = query.Where(a => a.AthleteId == filter.AthleteId.Value);
        }

        if (filter.StartDate is not null)
        {
            query = query.Where(a => a.TrainingSession.SessionDate >= filter.StartDate.Value);
        }

        if (filter.EndDate is not null)
        {
            query = query.Where(a => a.TrainingSession.SessionDate <= filter.EndDate.Value);
        }

        var records = await query
            .Select(a => new
            {
                a.AthleteId,
                a.PrivateSessionAthleteId,
                a.AthleteCodeSnapshot,
                a.AthleteNameSnapshot,
                Nickname = a.AthleteId.HasValue ? a.Athlete!.Nickname : null,
                GuestPhone = a.PrivateSessionAthlete != null ? a.PrivateSessionAthlete.GuestPhone : null,
                a.TrainingSessionId,
                a.TrainingSession.TrainingType,
                a.TrainingSession.SessionDate,
                a.Status,
                a.ArrivalTime,
                a.Remark,
            })
            .ToListAsync();

        var planQuery = _db.RoutineParticipationPlans.AsNoTracking().AsQueryable();
        if (filter.AthleteId is not null)
        {
            planQuery = planQuery.Where(plan => plan.AthleteId == filter.AthleteId.Value);
        }
        if (filter.StartDate is not null)
        {
            planQuery = planQuery.Where(plan => plan.TrainingDate >= filter.StartDate.Value);
        }
        if (filter.EndDate is not null)
        {
            planQuery = planQuery.Where(plan => plan.TrainingDate <= filter.EndDate.Value);
        }

        var plannedRecords = await planQuery
            .Select(plan => new
            {
                plan.AthleteId,
                plan.Athlete.AthleteCode,
                plan.Athlete.FullName,
                plan.Athlete.Nickname,
                plan.TrainingDate,
            })
            .ToListAsync();

        var attendanceGroups = records
            .GroupBy(
                r => PersonNameNormalizer.ToComparisonKey(r.Nickname ?? r.AthleteNameSnapshot),
                StringComparer.Ordinal)
            .ToDictionary(group => group.Key, StringComparer.Ordinal);
        var plannedGroups = plannedRecords
            .GroupBy(
                plan => PersonNameNormalizer.ToComparisonKey(plan.Nickname ?? plan.FullName),
                StringComparer.Ordinal)
            .ToDictionary(group => group.Key, StringComparer.Ordinal);

        var participantKeys = attendanceGroups.Keys
            .Union(plannedGroups.Keys, StringComparer.Ordinal);
        var items = participantKeys
            .Select(participantKey =>
            {
                var attendanceGroup = attendanceGroups.TryGetValue(participantKey, out var attendance)
                    ? attendance.ToList()
                    : [];
                var plannedGroup = plannedGroups.TryGetValue(participantKey, out var plans)
                    ? plans.ToList()
                    : [];
                var registeredAttendance = attendanceGroup.FirstOrDefault(record => record.AthleteId.HasValue);
                var firstPlan = plannedGroup.FirstOrDefault();
                var nickname = registeredAttendance?.Nickname ?? firstPlan?.Nickname;
                var fullName = registeredAttendance?.AthleteNameSnapshot
                    ?? firstPlan?.FullName
                    ?? attendanceGroup.First().AthleteNameSnapshot;

                return new AthleteAttendanceReportItemDto
                {
                    AthleteId = registeredAttendance?.AthleteId ?? firstPlan?.AthleteId,
                    IsGuest = firstPlan is null && attendanceGroup.All(record => record.AthleteId is null),
                    ParticipantKey = $"name-{participantKey}",
                    GuestPhone = attendanceGroup.Select(record => record.GuestPhone).FirstOrDefault(phone => !string.IsNullOrWhiteSpace(phone)),
                    AthleteCode = registeredAttendance?.AthleteCodeSnapshot ?? firstPlan?.AthleteCode ?? string.Empty,
                    FullName = PersonNameNormalizer.ToDisplayName(fullName),
                    Nickname = nickname is null ? null : PersonNameNormalizer.ToDisplayName(nickname),
                    RoutinePlannedDateCount = plannedGroup.Select(plan => plan.TrainingDate).Distinct().Count(),
                    RoutineAttendanceCount = attendanceGroup.Count(record => record.TrainingType == TrainingType.Routine),
                    PrivateAttendanceCount = attendanceGroup.Count(record => record.TrainingType == TrainingType.Private),
                    Records = attendanceGroup
                        .OrderByDescending(r => r.SessionDate)
                        .Select(r => new AthleteAttendanceRecordDto
                        {
                            TrainingSessionId = r.TrainingSessionId,
                            TrainingType = r.TrainingType,
                            SessionDate = r.SessionDate,
                            Status = r.Status,
                            ArrivalTime = r.ArrivalTime,
                            Remark = r.Remark,
                        })
                        .ToList(),
                };
            })
            .OrderBy(i => i.Nickname ?? i.FullName)
            .ToList();

        return new AthleteAttendanceReportResponseDto { Items = items };
    }
}
