using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

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

            var conflicts = await _conflictService.CheckRoutineTemplateOverlapAsync(
                dto.CoachId, dto.StartTime, dto.EndTime, dto.EffectiveStartDate);

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

            // FR-CONFLICT-005 — the override itself remains identifiable in history.
            await RecordConflictOverrideAsync(conflicts, overrideReason, routineScheduleId: schedule.RoutineScheduleId, actionByUserId);

            var generation = await GenerateOccurrencesAsync(
                schedule, coach, schedule.EffectiveStartDate, actionByUserId);

            await transaction.CommitAsync();

            return new RoutineScheduleSaveResult { Schedule = MapToDetail(schedule, coach), InitialGeneration = generation };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create routine schedule. Controller: RoutineSchedulesController Service: RoutineScheduleService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
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

        var conflicts = await _conflictService.CheckRoutineTemplateOverlapAsync(
            dto.CoachId, dto.StartTime, dto.EndTime, dto.EffectiveStartDate,
            excludeRoutineScheduleId: routineScheduleId);

        var overrideReason = conflicts.Count > 0 && dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
        if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
        {
            return new RoutineScheduleSaveResult { Error = "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", Conflicts = conflicts };
        }

        // FR-ROUTINE-007: only the schedule template changes here — already generated
        // TrainingSession rows (past or future) are never modified by this update.
        schedule.CoachId = dto.CoachId;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.EffectiveStartDate = dto.EffectiveStartDate;
        schedule.Remarks = dto.Remarks;
        schedule.UpdatedByUserId = actionByUserId;
        schedule.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // FR-CONFLICT-005 — the override itself remains identifiable in history.
        await RecordConflictOverrideAsync(conflicts, overrideReason, routineScheduleId: schedule.RoutineScheduleId, actionByUserId);

        return new RoutineScheduleSaveResult { Schedule = MapToDetail(schedule, coach) };
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

    private async Task<GenerateSessionsResult> GenerateOccurrencesAsync(RoutineSchedule schedule, Coach coach, DateOnly throughDateRequested, int actionByUserId)
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
        if (conflicts.Count > 0)
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

        foreach (var _ in conflicts)
        {
            _db.ConflictOverrideHistories.Add(new ConflictOverrideHistory
            {
                ConflictType = ConflictType.CoachOverlap,
                RoutineScheduleId = routineScheduleId,
                Reason = overrideReason,
                ActionByUserId = actionByUserId,
            });
        }

        await _db.SaveChangesAsync();
    }
}
