using CoachTraining.Api.DTOs.PrivateSessions;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class PrivateSessionServiceTests
{
    private static PrivateSessionService CreateService(Data.ApplicationDbContext db) =>
        new(db, new ScheduleConflictService(db), NullLogger<PrivateSessionService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<Athlete> SeedAthleteAsync(Data.ApplicationDbContext db, string code = "A001")
    {
        var athlete = new Athlete { AthleteCode = code, FullName = $"Athlete {code}", IsActive = true };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync();
        return athlete;
    }

    private static PrivateSessionCreateDto BuildCreateDto(int coachId, List<int> athleteIds, DateOnly date, TimeOnly start, TimeOnly end) => new()
    {
        CoachId = coachId,
        AthleteIds = athleteIds,
        SessionDate = date,
        StartTime = start,
        EndTime = end,
    };

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesSessionWithAssignedAthletes()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.Equal(TrainingType.Private, (await db.TrainingSessions.FirstAsync()).TrainingType);
        Assert.Equal(SessionStatus.Scheduled, result.Session!.Status);
        Assert.Single(result.Session.Athletes);
        Assert.Equal(athlete.AthleteCode, result.Session.Athletes[0].AthleteCode);
    }

    [Fact]
    public async Task CreateAsync_WithNoAthletes_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
        Assert.Empty(await db.TrainingSessions.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WithInactiveCoach_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        coach.IsActive = false;
        await db.SaveChangesAsync();
        var athlete = await SeedAthleteAsync(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveAthlete_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        athlete.IsActive = false;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownAthlete_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [999], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateAsync_WithCoachDoubleBooked_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var service = CreateService(db);

        var first = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athleteA.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);
        Assert.Null(first.Error);

        var second = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athleteB.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(18, 0), new TimeOnly(20, 0)),
            actionByUserId: 1);

        Assert.Null(second.Session);
        Assert.NotEmpty(second.Conflicts);
        Assert.Equal(1, await db.TrainingSessions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithAthleteDoubleBooked_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coachA = await SeedCoachAsync(db, "C001");
        var coachB = await SeedCoachAsync(db, "C002");
        var athlete = await SeedAthleteAsync(db);
        var service = CreateService(db);

        var first = await service.CreateAsync(
            BuildCreateDto(coachA.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);
        Assert.Null(first.Error);

        // Different coach, same athlete, overlapping time.
        var second = await service.CreateAsync(
            BuildCreateDto(coachB.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(18, 0), new TimeOnly(20, 0)),
            actionByUserId: 1);

        Assert.Null(second.Session);
        Assert.Contains(second.Conflicts, c => c.AthleteId == athlete.AthleteId);
    }

    [Fact]
    public async Task CreateAsync_WhenCoachHasOverlappingRoutineSession_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var date = new DateOnly(2026, 1, 5);

        // A Routine Training session already occupies the coach's time.
        db.TrainingSessions.Add(new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = SessionStatus.Scheduled,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athlete.AthleteId], date, new TimeOnly(18, 0), new TimeOnly(20, 0)),
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotEmpty(result.Conflicts);
    }

    [Fact]
    public async Task CreateAsync_WithCoachConflictAndNoOverrideReason_ReturnsConflicts()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var service = CreateService(db);

        await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athleteA.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        // FR-CONFLICT-004: OverrideConflict alone, without a reason, must not bypass the conflict.
        var dto = BuildCreateDto(coach.CoachId, [athleteB.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(18, 0), new TimeOnly(20, 0));
        dto.OverrideConflict = true;
        var result = await service.CreateAsync(dto, actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotEmpty(result.Conflicts);
        Assert.Equal(1, await db.TrainingSessions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithCoachConflictAndOverrideReason_PersistsSessionAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var service = CreateService(db);

        await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athleteA.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        var dto = BuildCreateDto(coach.CoachId, [athleteB.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(18, 0), new TimeOnly(20, 0));
        dto.OverrideConflict = true;
        dto.OverrideReason = "ผู้บริหารอนุมัติให้สอนซ้อนเวลา";
        var result = await service.CreateAsync(dto, actionByUserId: 7);

        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.True(result.Session!.IsConflictOverridden);
        Assert.Equal("ผู้บริหารอนุมัติให้สอนซ้อนเวลา", result.Session.ConflictOverrideReason);
        Assert.Equal(2, await db.TrainingSessions.CountAsync());

        var overrideHistory = await db.ConflictOverrideHistories.SingleAsync();
        Assert.Equal(result.Session.TrainingSessionId, overrideHistory.TrainingSessionId);
        Assert.Equal(7, overrideHistory.ActionByUserId);
    }

    [Fact]
    public async Task UpdateAsync_OnNonScheduledSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athlete.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        var session = await db.TrainingSessions.FirstAsync(s => s.TrainingSessionId == created.Session!.TrainingSessionId);
        session.Status = SessionStatus.Completed;
        await db.SaveChangesAsync();

        var result = await service.UpdateAsync(
            created.Session!.TrainingSessionId,
            new PrivateSessionUpdateDto { CoachId = coach.CoachId, AthleteIds = [athlete.AthleteId], SessionDate = new DateOnly(2026, 1, 5), StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0) },
            actionByUserId: 1);

        Assert.Null(result.Session);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesAthleteAssignments()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var service = CreateService(db);

        var created = await service.CreateAsync(
            BuildCreateDto(coach.CoachId, [athleteA.AthleteId], new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0)),
            actionByUserId: 1);

        var result = await service.UpdateAsync(
            created.Session!.TrainingSessionId,
            new PrivateSessionUpdateDto { CoachId = coach.CoachId, AthleteIds = [athleteB.AthleteId], SessionDate = new DateOnly(2026, 1, 5), StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0) },
            actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        var athleteIds = result.Session!.Athletes.Select(a => a.AthleteId).ToList();
        Assert.DoesNotContain(athleteA.AthleteId, athleteIds);
        Assert.Contains(athleteB.AthleteId, athleteIds);

        var assignmentCount = await db.PrivateSessionAthletes.CountAsync(psa => psa.TrainingSessionId == created.Session.TrainingSessionId);
        Assert.Equal(1, assignmentCount);
    }
}
