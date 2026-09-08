using CoachTraining.Api.DTOs.Reschedules;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class ReschedulingServiceTests
{
    private static ReschedulingService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), new ScheduleConflictService(db), NullLogger<ReschedulingService>.Instance);

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

    private static async Task<TrainingSession> SeedSessionAsync(
        Data.ApplicationDbContext db,
        Coach coach,
        SessionStatus status = SessionStatus.Scheduled,
        TrainingType trainingType = TrainingType.Routine)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = trainingType,
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

    private static RescheduleSessionRequest BuildRequest(DateOnly date, TimeOnly start, TimeOnly end) => new()
    {
        SessionDate = date,
        StartTime = start,
        EndTime = end,
    };

    [Fact]
    public async Task RescheduleAsync_WithValidData_PreservesOriginalAndCreatesLinkedReplacement()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var original = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.RescheduleAsync(
            original.TrainingSessionId,
            BuildRequest(new DateOnly(2026, 1, 8), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.OriginalSession);
        Assert.NotNull(result.ReplacementSession);

        Assert.Equal(SessionStatus.Rescheduled, result.OriginalSession!.Status);
        Assert.Equal(SessionStatus.Scheduled, result.ReplacementSession!.Status);
        Assert.Equal(original.TrainingSessionId, result.ReplacementSession.OriginalSessionId);
        Assert.Equal(new DateOnly(2026, 1, 8), result.ReplacementSession.SessionDate);

        Assert.Equal(2, await db.TrainingSessions.CountAsync());
    }

    [Fact]
    public async Task RescheduleAsync_CarriesOverPrivateAthleteAssignments()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var original = await SeedSessionAsync(db, coach, trainingType: TrainingType.Private);
        original.PrivateAthletes.Add(new PrivateSessionAthlete
        {
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RescheduleAsync(
            original.TrainingSessionId,
            BuildRequest(new DateOnly(2026, 1, 8), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        Assert.Null(result.Error);
        var replacementAssignments = await db.PrivateSessionAthletes
            .Where(psa => psa.TrainingSessionId == result.ReplacementSession!.TrainingSessionId)
            .ToListAsync();
        Assert.Single(replacementAssignments);
        Assert.Equal(athlete.AthleteId, replacementAssignments[0].AthleteId);
    }

    [Theory]
    [InlineData(SessionStatus.InProgress)]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Submitted)]
    [InlineData(SessionStatus.Locked)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Rescheduled)]
    public async Task RescheduleAsync_WhenSessionNotEligible_ReturnsError(SessionStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var original = await SeedSessionAsync(db, coach, status);
        var service = CreateService(db);

        var result = await service.RescheduleAsync(
            original.TrainingSessionId,
            BuildRequest(new DateOnly(2026, 1, 8), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.ReplacementSession);
        Assert.Equal(1, await db.TrainingSessions.CountAsync());
    }

    [Fact]
    public async Task RescheduleAsync_WhenCoachDoubleBookedAtNewTime_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var original = await SeedSessionAsync(db, coach);
        await SeedSessionAsync(db, coach, status: SessionStatus.Scheduled); // occupies 2026-01-05 17:00-19:00 already, but same as original's own slot
        var service = CreateService(db);

        var result = await service.RescheduleAsync(
            original.TrainingSessionId,
            BuildRequest(new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        Assert.NotEmpty(result.Conflicts);
        Assert.Null(result.ReplacementSession);
        Assert.Equal(2, await db.TrainingSessions.CountAsync());
        Assert.Equal(SessionStatus.Scheduled, (await db.TrainingSessions.FindAsync(original.TrainingSessionId))!.Status);
    }

    [Fact]
    public async Task RescheduleAsync_WithConflictAndOverrideReason_PersistsReplacementAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var original = await SeedSessionAsync(db, coach);
        await SeedSessionAsync(db, coach, status: SessionStatus.Scheduled);
        var service = CreateService(db);

        var request = BuildRequest(new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0));
        request.OverrideConflict = true;
        request.OverrideReason = "ผู้บริหารอนุมัติให้เลื่อนซ้อนเวลา";

        var result = await service.RescheduleAsync(original.TrainingSessionId, request, actionByUserId: 5);

        Assert.Null(result.Error);
        Assert.NotNull(result.ReplacementSession);
        Assert.True(result.ReplacementSession!.IsConflictOverridden);

        var overrideHistory = await db.ConflictOverrideHistories.SingleAsync();
        Assert.Equal(result.ReplacementSession.TrainingSessionId, overrideHistory.TrainingSessionId);
        Assert.Equal(5, overrideHistory.ActionByUserId);
    }

    [Fact]
    public async Task RescheduleAsync_WhenSessionNotFound_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.RescheduleAsync(
            999,
            BuildRequest(new DateOnly(2026, 1, 8), new TimeOnly(17, 0), new TimeOnly(19, 0)),
            actionByUserId: 1);

        Assert.True(result.NotFound);
    }
}
