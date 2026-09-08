using CoachTraining.Api.DTOs.RoutineSchedules;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class RoutineScheduleServiceTests
{
    private static RoutineScheduleService CreateService(Data.ApplicationDbContext db) =>
        new(db, new ScheduleConflictService(db), NullLogger<RoutineScheduleService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    [Fact]
    public async Task ListAsync_OrdersSchedulesByTrainingDateThenTime()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        db.RoutineSchedules.AddRange(
            new RoutineSchedule
            {
                CoachId = coach.CoachId,
                EffectiveStartDate = new DateOnly(2026, 9, 3),
                StartTime = new TimeOnly(18, 30),
                EndTime = new TimeOnly(20, 30),
            },
            new RoutineSchedule
            {
                CoachId = coach.CoachId,
                EffectiveStartDate = new DateOnly(2026, 9, 2),
                StartTime = new TimeOnly(19, 0),
                EndTime = new TimeOnly(20, 30),
            },
            new RoutineSchedule
            {
                CoachId = coach.CoachId,
                EffectiveStartDate = new DateOnly(2026, 9, 2),
                StartTime = new TimeOnly(18, 30),
                EndTime = new TimeOnly(20, 30),
            });
        await db.SaveChangesAsync();

        var result = await CreateService(db).ListAsync(new PagedRequest
        {
            Page = 1,
            PageSize = 20,
        });

        Assert.Equal(
            [
                (new DateOnly(2026, 9, 2), new TimeOnly(18, 30)),
                (new DateOnly(2026, 9, 2), new TimeOnly(19, 0)),
                (new DateOnly(2026, 9, 3), new TimeOnly(18, 30)),
            ],
            result.Items.Select(x => (x.EffectiveStartDate, x.StartTime)).ToArray());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesScheduleAndGeneratesInitialSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);

        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var dto = new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        };

        var result = await service.CreateAsync(dto, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Schedule);
        Assert.NotNull(result.InitialGeneration);
        Assert.Equal(1, result.InitialGeneration!.GeneratedCount);

        var generatedSession = await db.TrainingSessions.FirstAsync(s => s.RoutineScheduleId == result.Schedule!.RoutineScheduleId);
        Assert.Equal(TrainingType.Routine, generatedSession.TrainingType);
        Assert.Equal(SessionStatus.Scheduled, generatedSession.Status);
        Assert.Equal(coach.CoachCode, generatedSession.AssignedCoachCodeSnapshot);
        Assert.Equal(effectiveStart, generatedSession.SessionDate);
        Assert.Single(await db.TrainingSessions.Where(s => s.RoutineScheduleId == result.Schedule.RoutineScheduleId).ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WithInactiveCoach_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        coach.IsActive = false;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        }, actionByUserId: 1);

        Assert.Null(result.Schedule);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownCoach_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = 999,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        }, actionByUserId: 1);

        Assert.Null(result.Schedule);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithOverlappingRoutineTemplate_ReturnsConflictsAndDoesNotPersist()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var first = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);
        Assert.Null(first.Error);

        var second = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(20, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        Assert.Null(second.Schedule);
        Assert.NotEmpty(second.Conflicts);

        var scheduleCount = await db.RoutineSchedules.CountAsync();
        Assert.Equal(1, scheduleCount);
    }

    [Fact]
    public async Task CreateAsync_WithOverlapAndNoOverrideReason_ReturnsConflicts()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        // FR-CONFLICT-004: OverrideConflict alone, without a reason, must not bypass the conflict.
        var second = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(20, 0),
            EffectiveStartDate = effectiveStart,
            OverrideConflict = true,
        }, actionByUserId: 1);

        Assert.Null(second.Schedule);
        Assert.NotEmpty(second.Conflicts);
        Assert.Equal(1, await db.RoutineSchedules.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithOverlapAndOverrideReason_PersistsScheduleAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        var second = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(20, 0),
            EffectiveStartDate = effectiveStart,
            OverrideConflict = true,
            OverrideReason = "โค้ชยืนยันสอนสองกลุ่มพร้อมกัน",
        }, actionByUserId: 42);

        Assert.Null(second.Error);
        Assert.NotNull(second.Schedule);
        Assert.Equal(2, await db.RoutineSchedules.CountAsync());

        var overrideHistory = await db.ConflictOverrideHistories.SingleAsync();
        Assert.Equal(second.Schedule!.RoutineScheduleId, overrideHistory.RoutineScheduleId);
        Assert.Equal("โค้ชยืนยันสอนสองกลุ่มพร้อมกัน", overrideHistory.Reason);
        Assert.Equal(42, overrideHistory.ActionByUserId);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotModifyAlreadyGeneratedSessionTimes()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        var generatedSession = await db.TrainingSessions.FirstAsync(s => s.RoutineScheduleId == created.Schedule!.RoutineScheduleId);
        // Simulate the session having already been taught and finalized.
        generatedSession.Status = SessionStatus.Completed;
        generatedSession.ActualStartDateTime = generatedSession.ScheduledStartDateTime;
        generatedSession.ActualEndDateTime = generatedSession.ScheduledEndDateTime;
        await db.SaveChangesAsync();
        var originalStart = generatedSession.ScheduledStartDateTime;
        var originalEnd = generatedSession.ScheduledEndDateTime;

        // Change the schedule's time — completed historical sessions must not move (FR-ROUTINE-007).
        var updateResult = await service.UpdateAsync(created.Schedule!.RoutineScheduleId, new RoutineScheduleUpdateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        Assert.Null(updateResult.Error);

        var reloadedSession = await db.TrainingSessions.AsNoTracking().FirstAsync(s => s.TrainingSessionId == generatedSession.TrainingSessionId);
        Assert.Equal(originalStart, reloadedSession.ScheduledStartDateTime);
        Assert.Equal(originalEnd, reloadedSession.ScheduledEndDateTime);
        Assert.Equal(SessionStatus.Completed, reloadedSession.Status);
    }

    [Fact]
    public async Task GenerateSessionsAsync_CalledTwice_DoesNotDuplicateExistingDates()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        var countAfterCreate = await db.TrainingSessions.CountAsync();

        // Re-run generation through the selected date — should be a no-op.
        var (result, error) = await service.GenerateSessionsAsync(
            created.Schedule!.RoutineScheduleId,
            effectiveStart,
            actionByUserId: 1);

        Assert.Null(error);
        Assert.Equal(0, result!.GeneratedCount);
        Assert.Equal(countAfterCreate, await db.TrainingSessions.CountAsync());
    }

    [Fact]
    public async Task GenerateSessionsAsync_SkipsDateWithExistingCoachConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var occurrenceDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        // A pre-existing session occupies the coach's time on the very first occurrence date.
        db.TrainingSessions.Add(new TrainingSession
        {
            TrainingType = TrainingType.Private,
            SessionDate = occurrenceDate,
            ScheduledStartDateTime = occurrenceDate.ToDateTime(new TimeOnly(17, 30)),
            ScheduledEndDateTime = occurrenceDate.ToDateTime(new TimeOnly(18, 30)),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = SessionStatus.Scheduled,
        });
        await db.SaveChangesAsync();

        var result = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = occurrenceDate,
        }, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.Contains(result.InitialGeneration!.SkippedDueToConflict, s => s.SessionDate == occurrenceDate);

        var hasRoutineSessionOnConflictDate = await db.TrainingSessions.AnyAsync(s =>
            s.RoutineScheduleId == result.Schedule!.RoutineScheduleId && s.SessionDate == occurrenceDate);
        Assert.False(hasRoutineSessionOnConflictDate);
    }

    [Fact]
    public async Task DeleteAsync_WithUntouchedScheduledSession_RemovesSessionAndHidesSchedule()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var selectedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = selectedDate,
        }, actionByUserId: 1);

        var (found, error) = await service.DeleteAsync(
            created.Schedule!.RoutineScheduleId, actionByUserId: 1);

        Assert.True(found);
        Assert.Null(error);
        Assert.False(await db.TrainingSessions.AnyAsync(s =>
            s.RoutineScheduleId == created.Schedule.RoutineScheduleId));
        Assert.False(await db.RoutineSchedules.AnyAsync(rs =>
            rs.RoutineScheduleId == created.Schedule.RoutineScheduleId));
        Assert.True(await db.RoutineSchedules.IgnoreQueryFilters().AnyAsync(rs =>
            rs.RoutineScheduleId == created.Schedule.RoutineScheduleId && rs.IsDeleted));
    }

    [Fact]
    public async Task DeleteOwnAsync_OnlyDeletesTheAuthenticatedCoachsSchedule()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var service = CreateService(db);
        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = owner.CoachId,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(20, 0),
            EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
        }, actionByUserId: 1);

        var forbidden = await service.DeleteOwnAsync(
            created.Schedule!.RoutineScheduleId, otherCoach.CoachId, actionByUserId: 2);
        Assert.True(forbidden.Found);
        Assert.True(forbidden.Forbidden);
        Assert.True(await db.RoutineSchedules.AnyAsync(x => x.RoutineScheduleId == created.Schedule.RoutineScheduleId));

        var deleted = await service.DeleteOwnAsync(
            created.Schedule.RoutineScheduleId, owner.CoachId, actionByUserId: 1);
        Assert.True(deleted.Found);
        Assert.False(deleted.Forbidden);
        Assert.Null(deleted.Error);
        Assert.False(await db.RoutineSchedules.AnyAsync(x => x.RoutineScheduleId == created.Schedule.RoutineScheduleId));
    }

    [Fact]
    public async Task DeleteAsync_WithProgressedSession_ReturnsErrorAndPreservesData()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
        }, actionByUserId: 1);
        var session = await db.TrainingSessions.SingleAsync(s =>
            s.RoutineScheduleId == created.Schedule!.RoutineScheduleId);
        session.Status = SessionStatus.Completed;
        await db.SaveChangesAsync();

        var (found, error) = await service.DeleteAsync(
            created.Schedule!.RoutineScheduleId, actionByUserId: 1);

        Assert.True(found);
        Assert.NotNull(error);
        Assert.True(await db.TrainingSessions.AnyAsync(s =>
            s.RoutineScheduleId == created.Schedule.RoutineScheduleId));
        Assert.True(await db.RoutineSchedules.AnyAsync(rs =>
            rs.RoutineScheduleId == created.Schedule.RoutineScheduleId));
    }
}
