using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class TrainingSessionServiceTests
{
    private static TrainingSessionService CreateService(Data.ApplicationDbContext db) => new(db);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code)
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static TrainingSession BuildSession(
        Coach assignedCoach, DateOnly date, TimeOnly start, TimeOnly end,
        TrainingType type = TrainingType.Routine, SessionStatus status = SessionStatus.Scheduled, Coach? actualCoach = null) => new()
    {
        TrainingType = type,
        SessionDate = date,
        ScheduledStartDateTime = date.ToDateTime(start),
        ScheduledEndDateTime = date.ToDateTime(end),
        AssignedCoachId = assignedCoach.CoachId,
        AssignedCoachCodeSnapshot = assignedCoach.CoachCode,
        AssignedCoachNameSnapshot = assignedCoach.FullName,
        ActualCoachId = actualCoach?.CoachId,
        ActualCoachCodeSnapshot = actualCoach?.CoachCode,
        ActualCoachNameSnapshot = actualCoach?.FullName,
        Status = status,
    };

    [Fact]
    public async Task ListAsync_AsCoach_OnlyReturnsOwnSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coachA = await SeedCoachAsync(db, "C001");
        var coachB = await SeedCoachAsync(db, "C002");
        db.TrainingSessions.Add(BuildSession(coachA, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)));
        db.TrainingSessions.Add(BuildSession(coachB, new DateOnly(2026, 1, 6), new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListAsync(new TrainingSessionFilterRequest(), isPrivilegedRole: false, currentCoachId: coachA.CoachId);

        var item = Assert.Single(result.Items);
        Assert.Equal(coachA.CoachCode, item.AssignedCoachCode);
    }

    [Fact]
    public async Task ListAsync_AsCoach_IncludesSessionsWhereActingAsActualCoach()
    {
        using var db = TestDbContextFactory.Create();
        var assigned = await SeedCoachAsync(db, "C001");
        var substitute = await SeedCoachAsync(db, "C002");
        db.TrainingSessions.Add(BuildSession(assigned, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0), actualCoach: substitute));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListAsync(new TrainingSessionFilterRequest(), isPrivilegedRole: false, currentCoachId: substitute.CoachId);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ListAsync_AsUnlinkedCoachAccount_ReturnsEmptyRatherThanEverything()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        db.TrainingSessions.Add(BuildSession(coach, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListAsync(new TrainingSessionFilterRequest(), isPrivilegedRole: false, currentCoachId: null);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_AsAdministrator_SeesAllCoachesSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coachA = await SeedCoachAsync(db, "C001");
        var coachB = await SeedCoachAsync(db, "C002");
        db.TrainingSessions.Add(BuildSession(coachA, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)));
        db.TrainingSessions.Add(BuildSession(coachB, new DateOnly(2026, 1, 6), new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListAsync(new TrainingSessionFilterRequest(), isPrivilegedRole: true, currentCoachId: null);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task ListAsync_FiltersByTrainingTypeStatusAndDateRange()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        db.TrainingSessions.Add(BuildSession(coach, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0), TrainingType.Routine, SessionStatus.Scheduled));
        db.TrainingSessions.Add(BuildSession(coach, new DateOnly(2026, 1, 6), new TimeOnly(17, 0), new TimeOnly(19, 0), TrainingType.Private, SessionStatus.Completed));
        db.TrainingSessions.Add(BuildSession(coach, new DateOnly(2026, 2, 1), new TimeOnly(17, 0), new TimeOnly(19, 0), TrainingType.Private, SessionStatus.Completed));
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var byType = await service.ListAsync(new TrainingSessionFilterRequest { TrainingType = TrainingType.Private }, true, null);
        Assert.Equal(2, byType.TotalCount);

        var byStatus = await service.ListAsync(new TrainingSessionFilterRequest { Status = SessionStatus.Scheduled }, true, null);
        Assert.Equal(1, byStatus.TotalCount);

        var byDateRange = await service.ListAsync(
            new TrainingSessionFilterRequest { DateFrom = new DateOnly(2026, 1, 1), DateTo = new DateOnly(2026, 1, 31) }, true, null);
        Assert.Equal(2, byDateRange.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_AsCoach_RequestingAnotherCoachsSession_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var coachA = await SeedCoachAsync(db, "C001");
        var coachB = await SeedCoachAsync(db, "C002");
        var session = BuildSession(coachA, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0));
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coachB.CoachId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AsCoach_RequestingOwnSession_ReturnsDetail()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        var session = BuildSession(coach, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0));
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId);

        Assert.NotNull(result);
        Assert.Equal(session.TrainingSessionId, result!.TrainingSessionId);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesAssignedPrivateAthletes()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        var athlete = new Athlete { AthleteCode = "A001", FullName = "Athlete One", IsActive = true };
        db.Athletes.Add(athlete);
        var session = BuildSession(coach, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(18, 0), TrainingType.Private);
        session.PrivateAthletes.Add(new PrivateSessionAthlete { AthleteId = athlete.AthleteId, Athlete = athlete, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName });
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null);

        Assert.NotNull(result);
        var resultAthlete = Assert.Single(result!.Athletes);
        Assert.Equal("A001", resultAthlete.AthleteCode);
    }
}
