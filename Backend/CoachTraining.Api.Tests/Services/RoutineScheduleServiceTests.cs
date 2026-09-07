using CoachTraining.Api.DTOs.RoutineSchedules;
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

    /// <summary>The next date on/after <paramref name="from"/> that falls on <paramref name="dayOfWeek"/>.</summary>
    private static DateOnly NextDate(DateOnly from, DayOfWeek dayOfWeek)
    {
        var date = from;
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }
        return date;
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesScheduleAndGeneratesInitialSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);

        // At least a week out so the 28-day default window guarantees an occurrence.
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var dto = new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            DayOfWeek = effectiveStart.DayOfWeek,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        };

        var result = await service.CreateAsync(dto, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Schedule);
        Assert.NotNull(result.InitialGeneration);
        Assert.True(result.InitialGeneration!.GeneratedCount > 0);

        var generatedSession = await db.TrainingSessions.FirstAsync(s => s.RoutineScheduleId == result.Schedule!.RoutineScheduleId);
        Assert.Equal(TrainingType.Routine, generatedSession.TrainingType);
        Assert.Equal(SessionStatus.Scheduled, generatedSession.Status);
        Assert.Equal(coach.CoachCode, generatedSession.AssignedCoachCodeSnapshot);
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
            DayOfWeek = DayOfWeek.Monday,
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
            DayOfWeek = DayOfWeek.Monday,
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
            DayOfWeek = effectiveStart.DayOfWeek,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);
        Assert.Null(first.Error);

        var second = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            DayOfWeek = effectiveStart.DayOfWeek,
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
    public async Task UpdateAsync_DoesNotModifyAlreadyGeneratedSessionTimes()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);
        var effectiveStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var created = await service.CreateAsync(new RoutineScheduleCreateDto
        {
            CoachId = coach.CoachId,
            DayOfWeek = effectiveStart.DayOfWeek,
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
            DayOfWeek = effectiveStart.DayOfWeek,
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
            DayOfWeek = effectiveStart.DayOfWeek,
            StartTime = new TimeOnly(17, 0),
            EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = effectiveStart,
        }, actionByUserId: 1);

        var countAfterCreate = await db.TrainingSessions.CountAsync();

        // Re-run generation for the exact same window CreateAsync already covered
        // (today + the default 28-day horizon) — should be a no-op.
        var (result, error) = await service.GenerateSessionsAsync(
            created.Schedule!.RoutineScheduleId,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(28),
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
        var occurrenceDate = NextDate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), DayOfWeek.Monday);

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
            DayOfWeek = DayOfWeek.Monday,
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
}
