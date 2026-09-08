using CoachTraining.Api.DTOs.Substitutions;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class SubstituteCoachServiceTests
{
    private static SubstituteCoachService CreateService(Data.ApplicationDbContext db) =>
        new(db, new ScheduleConflictService(db), NullLogger<SubstituteCoachService>.Instance);

    private static async Task<Coach> SeedCoachAsync(
        Data.ApplicationDbContext db,
        string code,
        bool isActive = true)
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = isActive };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<TrainingSession> SeedSessionAsync(
        Data.ApplicationDbContext db,
        Coach assignedCoach,
        SessionStatus status = SessionStatus.Scheduled,
        TimeOnly? start = null,
        TimeOnly? end = null)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(start ?? new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(end ?? new TimeOnly(19, 0)),
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
    public async Task AssignAsync_PreservesOriginalCoachSetsActualCoachAndRecordsHistory()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, originalCoach);
        var service = CreateService(db);

        var result = await service.AssignAsync(
            session.TrainingSessionId,
            new SubstituteCoachRequest { SubstituteCoachId = substituteCoach.CoachId, Reason = "โค้ชเดิมลาป่วย" },
            actionByUserId: 42);

        Assert.Null(result.Error);
        Assert.Empty(result.Conflicts);
        Assert.NotNull(result.Data);
        Assert.Equal(originalCoach.CoachId, result.Data!.Session.AssignedCoachId);
        Assert.Equal(originalCoach.CoachCode, result.Data.Session.AssignedCoachCode);
        Assert.Equal(substituteCoach.CoachId, result.Data.Session.ActualCoachId);
        Assert.Equal(substituteCoach.CoachCode, result.Data.Session.ActualCoachCode);

        var savedSession = await db.TrainingSessions.SingleAsync();
        Assert.Equal(originalCoach.CoachId, savedSession.AssignedCoachId);
        Assert.Equal(substituteCoach.CoachId, savedSession.ActualCoachId);

        var history = await db.CoachSubstitutionHistories.SingleAsync();
        Assert.Equal(originalCoach.CoachId, history.OriginalCoachId);
        Assert.Equal(substituteCoach.CoachId, history.SubstituteCoachId);
        Assert.Equal("โค้ชเดิมลาป่วย", history.Reason);
        Assert.Equal(42, history.ActionByUserId);
    }

    [Fact]
    public async Task AssignAsync_WithInactiveSubstitute_ReturnsErrorWithoutChangingSession()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var inactiveCoach = await SeedCoachAsync(db, "C002", isActive: false);
        var session = await SeedSessionAsync(db, originalCoach);
        var service = CreateService(db);

        var result = await service.AssignAsync(
            session.TrainingSessionId,
            new SubstituteCoachRequest { SubstituteCoachId = inactiveCoach.CoachId, Reason = "แทนชั่วคราว" },
            actionByUserId: 42);

        Assert.NotNull(result.Error);
        Assert.Null(session.ActualCoachId);
        Assert.Empty(db.CoachSubstitutionHistories);
    }

    [Fact]
    public async Task AssignAsync_WithWhitespaceReason_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, originalCoach);
        var service = CreateService(db);

        var result = await service.AssignAsync(
            session.TrainingSessionId,
            new SubstituteCoachRequest { SubstituteCoachId = substituteCoach.CoachId, Reason = "   " },
            actionByUserId: 42);

        Assert.NotNull(result.Error);
        Assert.Null(result.Data);
        Assert.Empty(db.CoachSubstitutionHistories);
    }

    [Fact]
    public async Task AssignAsync_WhenSubstituteHasOverlappingSession_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var targetSession = await SeedSessionAsync(db, originalCoach);
        await SeedSessionAsync(db, substituteCoach, start: new TimeOnly(18, 0), end: new TimeOnly(20, 0));
        var service = CreateService(db);

        var result = await service.AssignAsync(
            targetSession.TrainingSessionId,
            new SubstituteCoachRequest { SubstituteCoachId = substituteCoach.CoachId, Reason = "แทนชั่วคราว" },
            actionByUserId: 42);

        Assert.NotEmpty(result.Conflicts);
        Assert.Null(targetSession.ActualCoachId);
        Assert.Empty(db.CoachSubstitutionHistories);
    }

    [Fact]
    public async Task AssignAsync_WithConflictAndOverrideReason_AssignsSubstituteAndRecordsOverrideHistory()
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var targetSession = await SeedSessionAsync(db, originalCoach);
        await SeedSessionAsync(db, substituteCoach, start: new TimeOnly(18, 0), end: new TimeOnly(20, 0));
        var service = CreateService(db);

        var result = await service.AssignAsync(
            targetSession.TrainingSessionId,
            new SubstituteCoachRequest
            {
                SubstituteCoachId = substituteCoach.CoachId,
                Reason = "แทนชั่วคราว",
                OverrideConflict = true,
                OverrideReason = "ผู้บริหารอนุมัติให้สอนซ้อนเวลา",
            },
            actionByUserId: 42);

        Assert.Null(result.Error);
        Assert.NotNull(result.Data);
        Assert.Equal(substituteCoach.CoachId, targetSession.ActualCoachId);
        Assert.Single(await db.CoachSubstitutionHistories.ToListAsync());

        var overrideHistory = await db.ConflictOverrideHistories.SingleAsync();
        Assert.Equal(targetSession.TrainingSessionId, overrideHistory.TrainingSessionId);
        Assert.Equal("ผู้บริหารอนุมัติให้สอนซ้อนเวลา", overrideHistory.Reason);
        Assert.Equal(42, overrideHistory.ActionByUserId);
    }

    [Theory]
    [InlineData(SessionStatus.InProgress)]
    [InlineData(SessionStatus.Completed)]
    [InlineData(SessionStatus.Submitted)]
    [InlineData(SessionStatus.Locked)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Rescheduled)]
    public async Task AssignAsync_WhenSessionAlreadyStartedOrFinalized_ReturnsError(SessionStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var originalCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, originalCoach, status);
        var service = CreateService(db);

        var result = await service.AssignAsync(
            session.TrainingSessionId,
            new SubstituteCoachRequest { SubstituteCoachId = substituteCoach.CoachId, Reason = "แทนชั่วคราว" },
            actionByUserId: 42);

        Assert.NotNull(result.Error);
        Assert.Null(session.ActualCoachId);
        Assert.Empty(db.CoachSubstitutionHistories);
    }
}
