using CoachTraining.Api.DTOs.Compensations;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CoachCompensationServiceTests
{
    private static CoachCompensationService CreateService(Data.ApplicationDbContext db) =>
        new(db, NullLogger<CoachCompensationService>.Instance);

    [Fact]
    public async Task CalculateAsync_UsesLockedPrivateSessionsAndCreditsActualCoach()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = new Coach
        {
            CoachCode = "C001",
            FullName = "Assigned Coach",
            BaseSalary = 20_000m,
            PrivateRate = 500m,
        };
        var actualCoach = new Coach
        {
            CoachCode = "C002",
            FullName = "Actual Coach",
            BaseSalary = 30_000m,
            PrivateRate = 800m,
        };
        db.Coaches.AddRange(assignedCoach, actualCoach);
        await db.SaveChangesAsync();

        var lockedPrivate = Session(assignedCoach, new DateOnly(2026, 10, 5), TrainingType.Private, SessionStatus.Locked);
        lockedPrivate.ActualCoachId = actualCoach.CoachId;
        lockedPrivate.ActualCoachCodeSnapshot = actualCoach.CoachCode;
        lockedPrivate.ActualCoachNameSnapshot = actualCoach.FullName;
        lockedPrivate.PrivateRateSnapshot = 750m;
        lockedPrivate.PrivateAthletes.Add(new PrivateSessionAthlete
        {
            IsGuest = true,
            AthleteCodeSnapshot = "GUEST",
            AthleteNameSnapshot = "Guest One",
            AthleteNicknameSnapshot = "One",
        });
        db.TrainingSessions.AddRange(
            lockedPrivate,
            Session(actualCoach, new DateOnly(2026, 10, 6), TrainingType.Routine, SessionStatus.Locked),
            Session(actualCoach, new DateOnly(2026, 10, 7), TrainingType.Private, SessionStatus.Completed));
        await db.SaveChangesAsync();

        var result = await CreateService(db).CalculateAsync(
            new CoachCompensationPeriodRequest { CoachId = actualCoach.CoachId, Year = 2026, Month = 10 },
            actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.Equal(30_000m, result.Data!.BaseSalary);
        Assert.Equal(1, result.Data.PrivateSessionCount);
        Assert.Equal(750m, result.Data.PrivateCompensation);
        Assert.Equal(30_750m, result.Data.NetAmount);
        Assert.Equal(lockedPrivate.TrainingSessionId, result.Data.Items.Single().TrainingSessionId);
        Assert.Equal("One", result.Data.Items.Single().ParticipantNames);
    }

    [Fact]
    public async Task DraftAdjustmentConfirmAndPaid_LocksTheStatementWorkflow()
    {
        using var db = TestDbContextFactory.Create();
        var coach = new Coach { CoachCode = "C001", FullName = "Coach A", BaseSalary = 25_000m };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var calculated = await service.CalculateAsync(
            new CoachCompensationPeriodRequest { CoachId = coach.CoachId, Year = 2026, Month = 10 },
            actionByUserId: 1);
        var statementId = calculated.Data!.CoachCompensationStatementId;

        var missingReason = await service.UpdateAdjustmentAsync(
            statementId,
            new CoachCompensationAdjustmentRequest { Amount = 500m },
            actionByUserId: 1);
        Assert.NotNull(missingReason.Error);

        var adjusted = await service.UpdateAdjustmentAsync(
            statementId,
            new CoachCompensationAdjustmentRequest { Amount = 500m, Reason = "ค่าเดินทาง" },
            actionByUserId: 1);
        Assert.Equal(25_500m, adjusted.Data!.NetAmount);

        var confirmed = await service.ConfirmAsync(statementId, actionByUserId: 1);
        Assert.Equal("Confirmed", confirmed.Data!.Status);

        var recalculate = await service.CalculateAsync(
            new CoachCompensationPeriodRequest { CoachId = coach.CoachId, Year = 2026, Month = 10 },
            actionByUserId: 1);
        Assert.NotNull(recalculate.Error);

        var paid = await service.MarkPaidAsync(statementId, actionByUserId: 1);
        Assert.Equal("Paid", paid.Data!.Status);
        Assert.NotNull(paid.Data.PaidDate);
    }

    private static TrainingSession Session(
        Coach coach,
        DateOnly date,
        TrainingType trainingType,
        SessionStatus status) => new()
    {
        TrainingType = trainingType,
        SessionDate = date,
        ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
        ScheduledEndDateTime = date.ToDateTime(new TimeOnly(18, 0)),
        AssignedCoachId = coach.CoachId,
        AssignedCoachCodeSnapshot = coach.CoachCode,
        AssignedCoachNameSnapshot = coach.FullName,
        Status = status,
    };
}
