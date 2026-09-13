using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class ScheduleConflictService : IScheduleConflictService
{
    /// <summary>Statuses that no longer occupy the coach's/athlete's time (FR-SESSION-009, FR-CR-006).</summary>
    private static readonly SessionStatus[] NonOccupyingStatuses = [SessionStatus.Cancelled, SessionStatus.Rescheduled];

    private readonly ApplicationDbContext _db;

    public ScheduleConflictService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ConflictDetail>> CheckCoachOverlapAsync(
        int coachId,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeTrainingSessionId = null)
    {
        var query = _db.TrainingSessions.Where(s =>
            (s.AssignedCoachId == coachId || s.ActualCoachId == coachId) &&
            !NonOccupyingStatuses.Contains(s.Status) &&
            s.ScheduledStartDateTime < endDateTime &&
            startDateTime < s.ScheduledEndDateTime);

        if (excludeTrainingSessionId is not null)
        {
            query = query.Where(s => s.TrainingSessionId != excludeTrainingSessionId.Value);
        }

        var overlapping = await query
            .Select(s => new { s.TrainingSessionId, s.SessionDate, s.ScheduledStartDateTime, s.ScheduledEndDateTime })
            .ToListAsync();

        return overlapping.Select(s => new ConflictDetail
        {
            ConflictType = nameof(ConflictType.CoachOverlap),
            ConflictingTrainingSessionId = s.TrainingSessionId,
            SessionDate = s.SessionDate,
            ScheduledStartDateTime = s.ScheduledStartDateTime,
            ScheduledEndDateTime = s.ScheduledEndDateTime,
            Message = $"โค้ชมีตารางฝึกซ้อมทับซ้อนในวันที่ {s.SessionDate:dd/MM/yyyy} เวลา {s.ScheduledStartDateTime:HH:mm}-{s.ScheduledEndDateTime:HH:mm}",
        }).ToList();
    }

    public async Task<List<ConflictDetail>> CheckAthleteOverlapAsync(
        IReadOnlyCollection<int> athleteIds,
        DateTime startDateTime,
        DateTime endDateTime,
        int? excludeTrainingSessionId = null)
    {
        if (athleteIds.Count == 0)
        {
            return [];
        }

        var query = _db.PrivateSessionAthletes
            .Where(psa => psa.AthleteId.HasValue && athleteIds.Contains(psa.AthleteId.Value))
            .Select(psa => new { psa.AthleteId, psa.AthleteNameSnapshot, Session = psa.TrainingSession })
            .Where(x =>
                !NonOccupyingStatuses.Contains(x.Session.Status) &&
                x.Session.ScheduledStartDateTime < endDateTime &&
                startDateTime < x.Session.ScheduledEndDateTime);

        if (excludeTrainingSessionId is not null)
        {
            query = query.Where(x => x.Session.TrainingSessionId != excludeTrainingSessionId.Value);
        }

        var overlapping = await query
            .Select(x => new
            {
                x.AthleteId,
                x.AthleteNameSnapshot,
                x.Session.TrainingSessionId,
                x.Session.SessionDate,
                x.Session.ScheduledStartDateTime,
                x.Session.ScheduledEndDateTime,
            })
            .ToListAsync();

        return overlapping.Select(x => new ConflictDetail
        {
            ConflictType = nameof(ConflictType.AthleteOverlap),
            ConflictingTrainingSessionId = x.TrainingSessionId,
            SessionDate = x.SessionDate,
            ScheduledStartDateTime = x.ScheduledStartDateTime,
            ScheduledEndDateTime = x.ScheduledEndDateTime,
            AthleteId = x.AthleteId,
            AthleteName = x.AthleteNameSnapshot,
            Message = $"นักกีฬา {x.AthleteNameSnapshot} มีตารางฝึกซ้อมส่วนตัวทับซ้อนในวันที่ {x.SessionDate:dd/MM/yyyy} เวลา {x.ScheduledStartDateTime:HH:mm}-{x.ScheduledEndDateTime:HH:mm}",
        }).ToList();
    }

    public async Task<List<RoutineTemplateConflictDetail>> CheckRoutineTemplateOverlapAsync(
        int coachId,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly effectiveStartDate,
        int? excludeRoutineScheduleId = null)
    {
        var query = _db.RoutineSchedules.Where(rs =>
            rs.CoachId == coachId &&
            rs.IsActive &&
            rs.EffectiveStartDate == effectiveStartDate &&
            rs.StartTime < endTime &&
            startTime < rs.EndTime);

        if (excludeRoutineScheduleId is not null)
        {
            query = query.Where(rs => rs.RoutineScheduleId != excludeRoutineScheduleId.Value);
        }

        var overlapping = await query
            .Select(rs => new { rs.RoutineScheduleId, rs.EffectiveStartDate, rs.StartTime, rs.EndTime })
            .ToListAsync();

        return overlapping
            .Select(rs => new RoutineTemplateConflictDetail
        {
            ConflictingRoutineScheduleId = rs.RoutineScheduleId,
            ConflictingRoutineScheduleName = BuildRoutineScheduleName(rs.EffectiveStartDate, rs.StartTime, rs.EndTime),
            Message = $"โค้ชมีตารางฝึกซ้อมทับซ้อนในวันที่ {effectiveStartDate:dd/MM/yyyy} เวลา {rs.StartTime:HH:mm}-{rs.EndTime:HH:mm}",
        }).ToList();
    }

    private static string BuildRoutineScheduleName(DateOnly effectiveStartDate, TimeOnly startTime, TimeOnly endTime) =>
        $"ฝึกซ้อมวันที่ {effectiveStartDate:dd/MM/yyyy} {startTime:HH:mm}-{endTime:HH:mm}";
}
