using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CoachTraining.Api.Services;

/// <summary>Routine Training Management (requirement.md 4.4, todo.md 4.3).</summary>
public class RoutineScheduleService : IRoutineScheduleService
{
    private readonly ApplicationDbContext _db;
    private readonly IScheduleConflictService _conflictService;
    private readonly ILogger<RoutineScheduleService> _logger;

    public RoutineScheduleService(ApplicationDbContext db, IScheduleConflictService conflictService, ILogger<RoutineScheduleService> logger)
    {
        _db = db;
        _conflictService = conflictService;
        _logger = logger;
    }

    public async Task<PagedResponse<RoutineScheduleListItemDto>> ListAsync(PagedRequest request)
    {
        var query = _db.RoutineSchedules.Include(rs => rs.Coach).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(rs =>
                rs.Coach.CoachCode.ToUpper().Contains(search) ||
                rs.Coach.FullName.ToUpper().Contains(search) ||
                (rs.Coach.Nickname != null && rs.Coach.Nickname.ToUpper().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(rs => rs.EffectiveStartDate)
            .ThenBy(rs => rs.StartTime)
            .ThenBy(rs => rs.Coach.Nickname)
            .ThenBy(rs => rs.Coach.CoachCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(rs => MapToListItem(rs))
            .ToListAsync();

        return new PagedResponse<RoutineScheduleListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<RoutineScheduleDetailDto?> GetByIdAsync(int routineScheduleId)
    {
        var schedule = await _db.RoutineSchedules.Include(rs => rs.Coach).FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        return schedule is null ? null : MapToDetail(schedule);
    }

    public async Task<RoutineScheduleSaveResult> CreateAsync(RoutineScheduleCreateDto dto, int actionByUserId)
    {
        try
        {
            var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == dto.CoachId);
            if (coach is null)
            {
                return new RoutineScheduleSaveResult { Error = "ไม่พบข้อมูลโค้ชที่เลือก" };
            }

            if (!coach.IsActive)
            {
                return new RoutineScheduleSaveResult { Error = "ไม่สามารถกำหนดตารางฝึกซ้อมให้โค้ชที่ปิดใช้งานได้" };
            }

            var scheduledStart = dto.EffectiveStartDate.ToDateTime(dto.StartTime);
            var scheduledEnd = dto.EffectiveStartDate.ToDateTime(dto.EndTime);
            var conflicts = await GetCreateConflictsAsync(
                dto.CoachId, dto.StartTime, dto.EndTime, dto.EffectiveStartDate, scheduledStart, scheduledEnd);

            // FR-CONFLICT-004: an authorized Administrator (this controller is
            // Administrator-only) may proceed past a detected conflict only by
            // supplying an override reason; otherwise the conflict still blocks.
            var overrideReason = conflicts.Count > 0 && dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
            if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
            {
                return new RoutineScheduleSaveResult { Error = "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", Conflicts = conflicts };
            }

            var schedule = new RoutineSchedule
            {
                CoachId = dto.CoachId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                EffectiveStartDate = dto.EffectiveStartDate,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };

            // Wrapped in a transaction so a failure while generating the initial
            // batch of sessions never leaves an orphaned, session-less schedule behind.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            _db.RoutineSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            var generation = await GenerateOccurrencesAsync(
                schedule, coach, schedule.EffectiveStartDate, actionByUserId,
                allowConflict: !string.IsNullOrWhiteSpace(overrideReason), overrideReason);

            // A RoutineSchedule without its operational TrainingSession is not a
            // valid completed create. This also protects against a conflict that
            // appears between the preflight query and session generation.
            if (generation.GeneratedCount != 1)
            {
                return new RoutineScheduleSaveResult
                {
                    Error = "ไม่สามารถสร้างรายการฝึกซ้อมได้ เนื่องจากพบตารางฝึกซ้อมของโค้ชทับซ้อนกัน",
                    Conflicts = generation.SkippedDueToConflict.Select(ToRoutineConflict).ToList(),
                };
            }

            // FR-CONFLICT-005 — the override itself remains identifiable in history.
            await RecordConflictOverrideAsync(conflicts, overrideReason, schedule.RoutineScheduleId, actionByUserId);

            await transaction.CommitAsync();

            return new RoutineScheduleSaveResult { Schedule = MapToDetail(schedule, coach), InitialGeneration = generation };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create routine schedule. Controller: RoutineSchedulesController Service: RoutineScheduleService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<(CoachRoutineScheduleBatchCreateResult? Result, string? Error, List<string> Conflicts)> CreateBatchAsync(
        int coachId, CoachRoutineScheduleBatchCreateDto dto, int actionByUserId)
    {
        var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == coachId);
        if (coach is null)
        {
            return (null, "ไม่พบข้อมูลโค้ชที่เลือก", []);
        }

        if (!coach.IsActive)
        {
            return (null, "ไม่สามารถกำหนดตารางฝึกซ้อมให้โค้ชที่ปิดใช้งานได้", []);
        }

        var selectedDays = dto.DaysOfWeek.ToHashSet();
        var dates = Enumerable.Range(0, dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1)
            .Select(offset => dto.StartDate.AddDays(offset))
            .Where(date => selectedDays.Contains(date.DayOfWeek))
            .ToList();

        if (dates.Count == 0)
        {
            return (null, "ไม่พบวันที่ตรงกับวันในสัปดาห์ที่เลือก", []);
        }

        var conflicts = new List<string>();
        foreach (var date in dates)
        {
            var dateConflicts = await _conflictService.CheckRoutineTemplateOverlapAsync(
                coachId, dto.StartTime, dto.EndTime, date);
            conflicts.AddRange(dateConflicts.Select(conflict => conflict.Message));
        }

        if (conflicts.Count > 0)
        {
            return (null, "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", conflicts);
        }

        var result = new CoachRoutineScheduleBatchCreateResult();
        foreach (var date in dates)
        {
            var saveResult = await CreateAsync(new RoutineScheduleCreateDto
            {
                CoachId = coachId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                EffectiveStartDate = date,
                Remarks = dto.Remarks,
            }, actionByUserId);

            if (saveResult.Error is not null)
            {
                return (null, saveResult.Error, saveResult.Conflicts.Select(conflict => conflict.Message).ToList());
            }

            result.CreatedDates.Add(date);
        }

        result.CreatedCount = result.CreatedDates.Count;
        return (result, null, []);
    }

    public async Task<RoutineScheduleSaveResult> UpdateAsync(int routineScheduleId, RoutineScheduleUpdateDto dto, int actionByUserId)
    {
        var schedule = await _db.RoutineSchedules.Include(rs => rs.Coach).FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        if (schedule is null)
        {
            return new RoutineScheduleSaveResult();
        }

        var coach = schedule.Coach;
        if (dto.CoachId != schedule.CoachId)
        {
            coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == dto.CoachId);
            if (coach is null)
            {
                return new RoutineScheduleSaveResult { Error = "ไม่พบข้อมูลโค้ชที่เลือก" };
            }

            if (!coach.IsActive)
            {
                return new RoutineScheduleSaveResult { Error = "ไม่สามารถกำหนดตารางฝึกซ้อมให้โค้ชที่ปิดใช้งานได้" };
            }
        }

        var editableSession = await _db.TrainingSessions
            .FirstOrDefaultAsync(session =>
                session.RoutineScheduleId == routineScheduleId &&
                session.Status == SessionStatus.Scheduled);

        var conflicts = await _conflictService.CheckRoutineTemplateOverlapAsync(
            dto.CoachId, dto.StartTime, dto.EndTime, dto.EffectiveStartDate,
            excludeRoutineScheduleId: routineScheduleId);

        if (editableSession is not null)
        {
            var scheduledStart = dto.EffectiveStartDate.ToDateTime(dto.StartTime);
            var scheduledEnd = dto.EffectiveStartDate.ToDateTime(dto.EndTime);
            var sessionConflicts = await _conflictService.CheckCoachOverlapAsync(
                dto.CoachId,
                scheduledStart,
                scheduledEnd,
                excludeTrainingSessionId: editableSession.TrainingSessionId);

            var conflictingRoutineScheduleIds = conflicts
                .Select(conflict => conflict.ConflictingRoutineScheduleId)
                .ToHashSet();
            conflicts.AddRange(sessionConflicts
                .Where(conflict => conflict.RoutineScheduleId is null ||
                    !conflictingRoutineScheduleIds.Contains(conflict.RoutineScheduleId.Value))
                .Select(ToRoutineConflict));
            conflicts = conflicts
                .GroupBy(conflict => conflict.Message)
                .Select(group => group.First())
                .ToList();
        }

        var overrideReason = conflicts.Count > 0 && dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
        if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
        {
            return new RoutineScheduleSaveResult { Error = "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", Conflicts = conflicts };
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // FR-ROUTINE-007: apply a changed Routine configuration to its still-Scheduled
        // operational session while preserving every progressed historical session.
        schedule.CoachId = dto.CoachId;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.EffectiveStartDate = dto.EffectiveStartDate;
        schedule.Remarks = dto.Remarks;
        schedule.UpdatedByUserId = actionByUserId;
        schedule.UpdatedDate = DateTime.UtcNow;

        if (editableSession is not null)
        {
            UpdateScheduledSession(editableSession, coach, dto, overrideReason, actionByUserId);
        }

        await _db.SaveChangesAsync();

        // FR-CONFLICT-005 — the override itself remains identifiable in history.
        await RecordConflictOverrideAsync(conflicts, overrideReason, routineScheduleId: schedule.RoutineScheduleId, actionByUserId);

        await transaction.CommitAsync();

        return new RoutineScheduleSaveResult { Schedule = MapToDetail(schedule, coach) };
    }

    private void UpdateScheduledSession(
        TrainingSession session,
        Coach coach,
        RoutineScheduleUpdateDto dto,
        string? overrideReason,
        int actionByUserId)
    {
        var previousValue = JsonSerializer.Serialize(new
        {
            session.SessionDate,
            session.ScheduledStartDateTime,
            session.ScheduledEndDateTime,
            session.AssignedCoachId,
            AssignedCoachCode = session.AssignedCoachCodeSnapshot,
            AssignedCoachName = session.AssignedCoachNameSnapshot,
            session.ActualCoachId,
            ActualCoachCode = session.ActualCoachCodeSnapshot,
            ActualCoachName = session.ActualCoachNameSnapshot,
        });
        var assignedCoachChanged = session.AssignedCoachId != coach.CoachId;

        session.SessionDate = dto.EffectiveStartDate;
        session.ScheduledStartDateTime = dto.EffectiveStartDate.ToDateTime(dto.StartTime);
        session.ScheduledEndDateTime = dto.EffectiveStartDate.ToDateTime(dto.EndTime);
        session.AssignedCoachId = coach.CoachId;
        session.AssignedCoachCodeSnapshot = coach.CoachCode;
        session.AssignedCoachNameSnapshot = coach.FullName;
        if (assignedCoachChanged)
        {
            session.ActualCoachId = null;
            session.ActualCoachCodeSnapshot = null;
            session.ActualCoachNameSnapshot = null;
        }

        session.IsConflictOverridden = overrideReason is not null;
        session.ConflictOverrideReason = overrideReason;
        session.UpdatedByUserId = actionByUserId;
        session.UpdatedDate = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(TrainingSession),
            EntityId = session.TrainingSessionId,
            Action = assignedCoachChanged ? "ReassignCoach" : "UpdateRoutineScheduleSession",
            PreviousValue = previousValue,
            NewValue = JsonSerializer.Serialize(new
            {
                session.SessionDate,
                session.ScheduledStartDateTime,
                session.ScheduledEndDateTime,
                session.AssignedCoachId,
                AssignedCoachCode = session.AssignedCoachCodeSnapshot,
                AssignedCoachName = session.AssignedCoachNameSnapshot,
                session.ActualCoachId,
            }),
            ActionByUserId = actionByUserId,
        });
    }

    public async Task<bool> SetStatusAsync(int routineScheduleId, bool isActive, int actionByUserId)
    {
        var schedule = await _db.RoutineSchedules.FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        if (schedule is null)
        {
            return false;
        }

        // Deactivating only stops future session generation — already generated
        // sessions (including future-dated ones) are left untouched.
        schedule.IsActive = isActive;
        schedule.UpdatedByUserId = actionByUserId;
        schedule.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Found, string? Error)> DeleteAsync(int routineScheduleId, int actionByUserId)
    {
        var schedule = await _db.RoutineSchedules
            .FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        if (schedule is null)
        {
            return (false, null);
        }

        var sessions = await _db.TrainingSessions
            .Where(s => s.RoutineScheduleId == routineScheduleId)
            .ToListAsync();

        if (sessions.Any(s => s.Status != SessionStatus.Scheduled))
        {
            return (true, "ไม่สามารถลบตารางที่เริ่มดำเนินการหรือมีประวัติการฝึกแล้วได้");
        }

        var sessionIds = sessions.Select(s => s.TrainingSessionId).ToList();
        if (sessionIds.Count > 0)
        {
            var hasHistory = await _db.Attendances.AnyAsync(x => sessionIds.Contains(x.TrainingSessionId)) ||
                await _db.TrainingLogs.AnyAsync(x => sessionIds.Contains(x.TrainingSessionId)) ||
                await _db.CoachSubstitutionHistories.AnyAsync(x => sessionIds.Contains(x.TrainingSessionId)) ||
                await _db.TrainingApprovalHistories.AnyAsync(x => sessionIds.Contains(x.TrainingSessionId)) ||
                await _db.ConflictOverrideHistories.AnyAsync(x =>
                    x.TrainingSessionId != null && sessionIds.Contains(x.TrainingSessionId.Value)) ||
                await _db.TrainingSessions.AnyAsync(x =>
                    x.OriginalSessionId != null && sessionIds.Contains(x.OriginalSessionId.Value));

            if (hasHistory)
            {
                return (true, "ไม่สามารถลบตารางที่มีข้อมูลการเข้าร่วม บันทึก หรือประวัติการดำเนินการแล้วได้");
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.TrainingSessions.RemoveRange(sessions);
        schedule.IsDeleted = true;
        schedule.IsActive = false;
        schedule.UpdatedByUserId = actionByUserId;
        schedule.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null);
    }

    public async Task<(bool Found, bool Forbidden, string? Error)> DeleteOwnAsync(
        int routineScheduleId, int coachId, int actionByUserId)
    {
        var schedule = await _db.RoutineSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        if (schedule is null)
        {
            return (false, false, null);
        }

        if (schedule.CoachId != coachId)
        {
            return (true, true, null);
        }

        var (found, error) = await DeleteAsync(routineScheduleId, actionByUserId);
        return (found, false, error);
    }

    public async Task<(GenerateSessionsResult? Result, string? Error)> GenerateSessionsAsync(int routineScheduleId, DateOnly throughDate, int actionByUserId)
    {
        var schedule = await _db.RoutineSchedules.Include(rs => rs.Coach).FirstOrDefaultAsync(rs => rs.RoutineScheduleId == routineScheduleId);
        if (schedule is null)
        {
            return (null, null);
        }

        if (!schedule.IsActive)
        {
            return (null, "ไม่สามารถสร้างรอบฝึกซ้อมให้ตารางที่ปิดใช้งานได้");
        }

        var result = await GenerateOccurrencesAsync(schedule, schedule.Coach, throughDate, actionByUserId);
        return (result, null);
    }

    private async Task<GenerateSessionsResult> GenerateOccurrencesAsync(
        RoutineSchedule schedule,
        Coach coach,
        DateOnly throughDateRequested,
        int actionByUserId,
        bool allowConflict = false,
        string? overrideReason = null)
    {
        var result = new GenerateSessionsResult();

        var sessionDate = schedule.EffectiveStartDate;
        if (throughDateRequested < sessionDate)
        {
            return result;
        }

        var alreadyExists = await _db.TrainingSessions.AnyAsync(s =>
            s.RoutineScheduleId == schedule.RoutineScheduleId &&
            s.SessionDate == sessionDate);
        if (alreadyExists)
        {
            return result;
        }

        var scheduledStart = sessionDate.ToDateTime(schedule.StartTime);
        var scheduledEnd = sessionDate.ToDateTime(schedule.EndTime);

        var conflicts = await _conflictService.CheckCoachOverlapAsync(
            schedule.CoachId, scheduledStart, scheduledEnd);
        if (conflicts.Count > 0 && !allowConflict)
        {
            result.SkippedDueToConflict.Add(new SkippedOccurrence
            {
                SessionDate = sessionDate,
                Reason = conflicts[0].Message,
            });
            return result;
        }

        _db.TrainingSessions.Add(new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            RoutineScheduleId = schedule.RoutineScheduleId,
            SessionDate = sessionDate,
            ScheduledStartDateTime = scheduledStart,
            ScheduledEndDateTime = scheduledEnd,
            AssignedCoachId = schedule.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = SessionStatus.Scheduled,
            IsConflictOverridden = conflicts.Count > 0,
            ConflictOverrideReason = conflicts.Count > 0 ? overrideReason : null,
            CreatedByUserId = actionByUserId,
        });
        result.GeneratedCount = 1;

        await _db.SaveChangesAsync();
        return result;
    }

    private static RoutineScheduleListItemDto MapToListItem(RoutineSchedule schedule) => new()
    {
        RoutineScheduleId = schedule.RoutineScheduleId,
        CoachId = schedule.CoachId,
        CoachCode = schedule.Coach.CoachCode,
        CoachFullName = schedule.Coach.FullName,
        CoachNickname = schedule.Coach.Nickname,
        CoachColorHex = schedule.Coach.ColorHex,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        EffectiveStartDate = schedule.EffectiveStartDate,
        IsActive = schedule.IsActive,
    };

    private static RoutineScheduleDetailDto MapToDetail(RoutineSchedule schedule) => MapToDetail(schedule, schedule.Coach);

    private static RoutineScheduleDetailDto MapToDetail(RoutineSchedule schedule, Coach coach) => new()
    {
        RoutineScheduleId = schedule.RoutineScheduleId,
        CoachId = schedule.CoachId,
        CoachCode = coach.CoachCode,
        CoachFullName = coach.FullName,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        EffectiveStartDate = schedule.EffectiveStartDate,
        IsActive = schedule.IsActive,
        Remarks = schedule.Remarks,
    };

    private async Task<List<RoutineTemplateConflictDetail>> GetCreateConflictsAsync(
        int coachId,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly sessionDate,
        DateTime scheduledStart,
        DateTime scheduledEnd)
    {
        var conflicts = await _conflictService.CheckRoutineTemplateOverlapAsync(
            coachId, startTime, endTime, sessionDate);
        var sessionConflicts = await _conflictService.CheckCoachOverlapAsync(
            coachId, scheduledStart, scheduledEnd);

        var conflictingRoutineScheduleIds = conflicts
            .Select(conflict => conflict.ConflictingRoutineScheduleId)
            .ToHashSet();

        conflicts.AddRange(sessionConflicts
            .Where(conflict => conflict.RoutineScheduleId is null ||
                !conflictingRoutineScheduleIds.Contains(conflict.RoutineScheduleId.Value))
            .Select(conflict => new RoutineTemplateConflictDetail
        {
            ConflictingRoutineScheduleId = 0,
            ConflictingRoutineScheduleName = $"รายการฝึกซ้อม #{conflict.ConflictingTrainingSessionId}",
            Message = conflict.Message,
        }));

        return conflicts
            .GroupBy(conflict => conflict.Message)
            .Select(group => group.First())
            .ToList();
    }

    private static RoutineTemplateConflictDetail ToRoutineConflict(SkippedOccurrence occurrence) => new()
    {
        ConflictingRoutineScheduleId = 0,
        ConflictingRoutineScheduleName = $"รายการฝึกซ้อมวันที่ {occurrence.SessionDate:dd/MM/yyyy}",
        Message = occurrence.Reason,
    };

    private static RoutineTemplateConflictDetail ToRoutineConflict(ConflictDetail conflict) => new()
    {
        ConflictingRoutineScheduleId = conflict.RoutineScheduleId ?? 0,
        ConflictingRoutineScheduleName = $"รายการฝึกซ้อม #{conflict.ConflictingTrainingSessionId}",
        Message = conflict.Message,
    };

    /// <summary>FR-CONFLICT-004/005 — records one history row per detected conflict once an
    /// authorized Administrator has supplied an override reason. No-op when there is
    /// nothing to override.</summary>
    private async Task RecordConflictOverrideAsync(
        List<RoutineTemplateConflictDetail> conflicts, string? overrideReason, int routineScheduleId, int actionByUserId)
    {
        if (conflicts.Count == 0 || string.IsNullOrWhiteSpace(overrideReason))
        {
            return;
        }

        var trainingSessionId = await _db.TrainingSessions
            .Where(session => session.RoutineScheduleId == routineScheduleId)
            .Select(session => (int?)session.TrainingSessionId)
            .SingleOrDefaultAsync();

        foreach (var _ in conflicts)
        {
            _db.ConflictOverrideHistories.Add(new ConflictOverrideHistory
            {
                ConflictType = ConflictType.CoachOverlap,
                TrainingSessionId = trainingSessionId,
                RoutineScheduleId = routineScheduleId,
                Reason = overrideReason,
                ActionByUserId = actionByUserId,
            });
        }

        await _db.SaveChangesAsync();
    }
}
