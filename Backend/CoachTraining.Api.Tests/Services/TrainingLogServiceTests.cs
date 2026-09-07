using CoachTraining.Api.DTOs.TrainingLogs;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class TrainingLogServiceTests
{
    private static TrainingLogService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<TrainingLogService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<TrainingSession> SeedSessionAsync(Data.ApplicationDbContext db, Coach coach, SessionStatus status = SessionStatus.InProgress)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = status,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task GetAsync_WithNoSavedLog_ReturnsDtoWithNullTrainingLogId()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.GetAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId);

        Assert.NotNull(result);
        Assert.Null(result!.TrainingLogId);
        Assert.Null(result.Topic);
    }

    [Fact]
    public async Task GetAsync_AsUnauthorizedCoach_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach);
        var service = CreateService(db);

        var result = await service.GetAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: otherCoach.CoachId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_WithNoExistingLog_CreatesOne()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.UpsertAsync(
            session.TrainingSessionId,
            new TrainingLogUpsertRequest { Topic = "Footwork", CoachNotes = "Good progress" },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.TrainingLog);
        Assert.NotNull(result.TrainingLog!.TrainingLogId);
        Assert.Equal("Footwork", result.TrainingLog.Topic);
    }

    [Fact]
    public async Task UpsertAsync_CalledTwice_UpdatesExistingLogRatherThanDuplicating()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        await service.UpsertAsync(session.TrainingSessionId, new TrainingLogUpsertRequest { Topic = "Footwork" }, false, coach.CoachId, 1);
        var second = await service.UpsertAsync(session.TrainingSessionId, new TrainingLogUpsertRequest { Topic = "Endurance" }, false, coach.CoachId, 1);

        Assert.Equal("Endurance", second.TrainingLog!.Topic);
        Assert.Single(db.TrainingLogs.Where(l => l.TrainingSessionId == session.TrainingSessionId));
    }

    [Fact]
    public async Task UpsertAsync_OnLockedSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.Locked);
        var service = CreateService(db);

        var result = await service.UpsertAsync(session.TrainingSessionId, new TrainingLogUpsertRequest { Topic = "x" }, false, coach.CoachId, 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.TrainingLog);
    }

    [Fact]
    public async Task UpsertAsync_ByAnotherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach);
        var service = CreateService(db);

        var result = await service.UpsertAsync(session.TrainingSessionId, new TrainingLogUpsertRequest { Topic = "x" }, false, otherCoach.CoachId, 1);

        Assert.True(result.Forbidden);
    }

    [Fact]
    public async Task UpsertAsync_OnNonExistentSession_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.UpsertAsync(999, new TrainingLogUpsertRequest(), isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.True(result.NotFound);
    }
}
