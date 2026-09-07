using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Routine Training Management (requirement.md 4.4, todo.md 4.3).</summary>
public class RoutineScheduleService : IRoutineScheduleService
{
    /// <summary>Default lookahead window generated automatically when a schedule is created.</summary>
    private const int DefaultGenerationHorizonDays = 28;

    /// <summary>Upper bound on a single generation request, to keep it a bounded, predictable operation.</summary>
    private const int MaxGenerationHorizonDays = 180;

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
                rs.Name.ToUpper().Contains(search) ||
                rs.Coach.CoachCode.ToUpper().Contains(search) ||
                rs.Coach.FullName.ToUpper().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(rs => rs.Coach.CoachCode)
            .ThenBy(rs => rs.DayOfWeek)
            .ThenBy(rs => rs.StartTime)
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
                dto.CoachId, dto.DayOfWeek, dto.StartTime, dto.EndTime, dto.EffectiveStartDate, dto.EffectiveEndDate);

            if (conflicts.Count > 0)
            {
                return new RoutineScheduleSaveResult { Error = "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", Conflicts = conflicts };
            }

            var schedule = new RoutineSchedule
            {
                Name = string.IsNullOrWhiteSpace(dto.Name) ? BuildDefaultName(dto.DayOfWeek, dto.StartTime, dto.EndTime) : dto.Name,
                CoachId = dto.CoachId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                EffectiveStartDate = dto.EffectiveStartDate,
                EffectiveEndDate = dto.EffectiveEndDate,
                RecurrencePattern = string.IsNullOrWhiteSpace(dto.RecurrencePattern) ? "Weekly" : dto.RecurrencePattern,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };

            // Wrapped in a transaction so a failure while generating the initial
            // batch of sessions never leaves an orphaned, session-less schedule behind.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            _db.RoutineSchedules.Add(schedule);
            await _db.SaveChangesAsync();

            var throughDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(DefaultGenerationHorizonDays);
            var generation = await GenerateOccurrencesAsync(schedule, coach, throughDate, actionByUserId);

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
            dto.CoachId, dto.DayOfWeek, dto.StartTime, dto.EndTime, dto.EffectiveStartDate, dto.EffectiveEndDate,
            excludeRoutineScheduleId: routineScheduleId);

        if (conflicts.Count > 0)
        {
            return new RoutineScheduleSaveResult { Error = "พบตารางฝึกซ้อมของโค้ชทับซ้อนกัน", Conflicts = conflicts };
        }

        // FR-ROUTINE-007: only the schedule template changes here — already generated
        // TrainingSession rows (past or future) are never modified by this update.
        schedule.Name = string.IsNullOrWhiteSpace(dto.Name) ? BuildDefaultName(dto.DayOfWeek, dto.StartTime, dto.EndTime) : dto.Name;
        schedule.CoachId = dto.CoachId;
        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;
        schedule.EffectiveStartDate = dto.EffectiveStartDate;
        schedule.EffectiveEndDate = dto.EffectiveEndDate;
        schedule.RecurrencePattern = string.IsNullOrWhiteSpace(dto.RecurrencePattern) ? "Weekly" : dto.RecurrencePattern;
        schedule.Remarks = dto.Remarks;
        schedule.UpdatedByUserId = actionByUserId;
        schedule.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeStart = schedule.EffectiveStartDate > today ? schedule.EffectiveStartDate : today;

        var rangeEnd = throughDateRequested;
        if (schedule.EffectiveEndDate is not null && schedule.EffectiveEndDate.Value < rangeEnd)
        {
            rangeEnd = schedule.EffectiveEndDate.Value;
        }
        var maxEnd = rangeStart.AddDays(MaxGenerationHorizonDays);
        if (rangeEnd > maxEnd)
        {
            rangeEnd = maxEnd;
        }

        if (rangeEnd < rangeStart)
        {
            return result;
        }

        var existingDates = (await _db.TrainingSessions
            .Where(s => s.RoutineScheduleId == schedule.RoutineScheduleId && s.SessionDate >= rangeStart && s.SessionDate <= rangeEnd)
            .Select(s => s.SessionDate)
            .ToListAsync())
            .ToHashSet();

        for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek != schedule.DayOfWeek || existingDates.Contains(date))
            {
                continue;
            }

            var scheduledStart = date.ToDateTime(schedule.StartTime);
            var scheduledEnd = date.ToDateTime(schedule.EndTime);

            var conflicts = await _conflictService.CheckCoachOverlapAsync(schedule.CoachId, scheduledStart, scheduledEnd);
            if (conflicts.Count > 0)
            {
                result.SkippedDueToConflict.Add(new SkippedOccurrence { SessionDate = date, Reason = conflicts[0].Message });
                continue;
            }

            _db.TrainingSessions.Add(new TrainingSession
            {
                TrainingType = TrainingType.Routine,
                RoutineScheduleId = schedule.RoutineScheduleId,
                SessionDate = date,
                ScheduledStartDateTime = scheduledStart,
                ScheduledEndDateTime = scheduledEnd,
                AssignedCoachId = schedule.CoachId,
                AssignedCoachCodeSnapshot = coach.CoachCode,
                AssignedCoachNameSnapshot = coach.FullName,
                Status = SessionStatus.Scheduled,
                CreatedByUserId = actionByUserId,
            });
            result.GeneratedCount++;
        }

        await _db.SaveChangesAsync();
        return result;
    }

    private static string BuildDefaultName(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime) =>
        $"ฝึกซ้อมวัน{ThaiDateHelper.DayName(dayOfWeek)} {startTime:HH:mm}-{endTime:HH:mm}";

    private static RoutineScheduleListItemDto MapToListItem(RoutineSchedule schedule) => new()
    {
        RoutineScheduleId = schedule.RoutineScheduleId,
        Name = schedule.Name,
        CoachId = schedule.CoachId,
        CoachCode = schedule.Coach.CoachCode,
        CoachFullName = schedule.Coach.FullName,
        DayOfWeek = schedule.DayOfWeek,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        EffectiveStartDate = schedule.EffectiveStartDate,
        EffectiveEndDate = schedule.EffectiveEndDate,
        IsActive = schedule.IsActive,
    };

    private static RoutineScheduleDetailDto MapToDetail(RoutineSchedule schedule) => MapToDetail(schedule, schedule.Coach);

    private static RoutineScheduleDetailDto MapToDetail(RoutineSchedule schedule, Coach coach) => new()
    {
        RoutineScheduleId = schedule.RoutineScheduleId,
        Name = schedule.Name,
        CoachId = schedule.CoachId,
        CoachCode = coach.CoachCode,
        CoachFullName = coach.FullName,
        DayOfWeek = schedule.DayOfWeek,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        EffectiveStartDate = schedule.EffectiveStartDate,
        EffectiveEndDate = schedule.EffectiveEndDate,
        RecurrencePattern = schedule.RecurrencePattern,
        IsActive = schedule.IsActive,
        Remarks = schedule.Remarks,
    };
}
