using CoachTraining.Api.DTOs.CoachTeaching;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CoachTeachingServiceTests
{
    private static CoachTeachingService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<CoachTeachingService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<TrainingSession> SeedSessionAsync(Data.ApplicationDbContext db, Coach assignedCoach, SessionStatus status = SessionStatus.Scheduled)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
            AssignedCoachId = assignedCoach.CoachId,
            AssignedCoachCodeSnapshot = assignedCoach.CoachCode,
            AssignedCoachNameSnapshot = assignedCoach.FullName,
            Status = status,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task StartAsync_ByAssignedCoach_TransitionsToInProgressAndSetsActualCoach()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.StartAsync(
            session.TrainingSessionId,
            new TeachingStartRequest { ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0) },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.False(result.NotFound);
        Assert.False(result.Forbidden);
        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.Equal(SessionStatus.InProgress, result.Session!.Status);
        Assert.Equal(coach.CoachId, result.Session.ActualCoachId);
        Assert.Equal(coach.CoachCode, result.Session.ActualCoachCode);
        Assert.Equal(new DateTime(2026, 1, 5, 17, 5, 0), result.Session.ActualStartDateTime);
        // Scheduled values must remain untouched (FR-TEACH-004).
        Assert.Equal(session.ScheduledStartDateTime, result.Session.ScheduledStartDateTime);
    }

    [Fact]
    public async Task StartAsync_ByAnotherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach);
        var service = CreateService(db);

        var result = await service.StartAsync(
            session.TrainingSessionId, new TeachingStartRequest(),
            isPrivilegedRole: false, currentCoachId: otherCoach.CoachId, actionByUserId: 1);

        Assert.True(result.Forbidden);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task StartAsync_WithPreassignedSubstitute_PreservesSubstituteAndRejectsOriginalCoach()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach);
        session.ActualCoachId = substituteCoach.CoachId;
        session.ActualCoachCodeSnapshot = substituteCoach.CoachCode;
        session.ActualCoachNameSnapshot = substituteCoach.FullName;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var assignedCoachResult = await service.StartAsync(
            session.TrainingSessionId,
            new TeachingStartRequest { ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0) },
            isPrivilegedRole: false,
            currentCoachId: assignedCoach.CoachId,
            actionByUserId: 1);

        Assert.True(assignedCoachResult.Forbidden);

        var substituteResult = await service.StartAsync(
            session.TrainingSessionId,
            new TeachingStartRequest { ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0) },
            isPrivilegedRole: false,
            currentCoachId: substituteCoach.CoachId,
            actionByUserId: 1);

        Assert.Null(substituteResult.Error);
        Assert.Equal(substituteCoach.CoachId, substituteResult.Session!.ActualCoachId);
        Assert.Equal(substituteCoach.CoachCode, substituteResult.Session.ActualCoachCode);
        Assert.Equal(assignedCoach.CoachId, substituteResult.Session.AssignedCoachId);
    }

    [Fact]
    public async Task StartAsync_ByAdministratorWithoutCoachLink_UsesAssignedCoachAsActual()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.StartAsync(
            session.TrainingSessionId, new TeachingStartRequest(),
            isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.Equal(coach.CoachId, result.Session!.ActualCoachId);
    }

    [Fact]
    public async Task StartAsync_OnAlreadyInProgressSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.InProgress);
        var service = CreateService(db);

        var result = await service.StartAsync(
            session.TrainingSessionId, new TeachingStartRequest(),
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task StartAsync_OnNonExistentSession_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.StartAsync(999, new TeachingStartRequest(), isPrivilegedRole: true, currentCoachId: null, actionByUserId: 1);

        Assert.True(result.NotFound);
    }

    [Fact]
    public async Task CompleteAsync_OnInProgressSession_TransitionsToCompletedAndComputesDuration()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.InProgress);
        session.ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0);
        session.ActualCoachId = coach.CoachId;
        session.ActualCoachCodeSnapshot = coach.CoachCode;
        session.ActualCoachNameSnapshot = coach.FullName;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAsync(
            session.TrainingSessionId,
            new TeachingEndRequest { ActualEndDateTime = new DateTime(2026, 1, 5, 18, 50, 0) },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Session);
        Assert.Equal(SessionStatus.Completed, result.Session!.Status);
        Assert.Equal(105, result.Session.ActualDurationMinutes);
        Assert.Equal(session.ScheduledEndDateTime, result.Session.ScheduledEndDateTime);
    }

    [Fact]
    public async Task CompleteAsync_BeforeStarting_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.CompleteAsync(
            session.TrainingSessionId, new TeachingEndRequest(),
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task CompleteAsync_WithEndAtOrBeforeStart_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach, SessionStatus.InProgress);
        session.ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAsync(
            session.TrainingSessionId,
            new TeachingEndRequest { ActualEndDateTime = new DateTime(2026, 1, 5, 17, 5, 0) },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Session);
    }

    [Fact]
    public async Task CompleteAsync_ByAnotherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach, SessionStatus.InProgress);
        session.ActualStartDateTime = new DateTime(2026, 1, 5, 17, 5, 0);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAsync(
            session.TrainingSessionId, new TeachingEndRequest(),
            isPrivilegedRole: false, currentCoachId: otherCoach.CoachId, actionByUserId: 1);

        Assert.True(result.Forbidden);
    }
}
