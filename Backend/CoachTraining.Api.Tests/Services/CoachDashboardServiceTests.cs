using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CoachDashboardServiceTests
{
    private static CoachDashboardService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService());

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static TrainingSession BuildSession(
        Coach coach, DateOnly date, TimeOnly start, TimeOnly end, SessionStatus status, TrainingType type = TrainingType.Routine,
        DateTime? actualStart = null, DateTime? actualEnd = null) => new()
    {
        TrainingType = type,
        SessionDate = date,
        ScheduledStartDateTime = date.ToDateTime(start),
        ScheduledEndDateTime = date.ToDateTime(end),
        ActualStartDateTime = actualStart,
        ActualEndDateTime = actualEnd,
        AssignedCoachId = coach.CoachId,
        AssignedCoachCodeSnapshot = coach.CoachCode,
        AssignedCoachNameSnapshot = coach.FullName,
        Status = status,
    };

    [Fact]
    public async Task GetDashboardAsync_SeparatesTodayFromUpcomingAndAttachesNextAction()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var now = new DateTime(2026, 1, 10, 9, 0, 0);
        var today = DateOnly.FromDateTime(now);

        db.TrainingSessions.AddRange(
            BuildSession(coach, today, new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Scheduled),
            BuildSession(coach, today.AddDays(2), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Scheduled),
            BuildSession(coach, today.AddDays(3), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Cancelled));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(coach.CoachId, now);

        Assert.Single(result.TodaySessions);
        Assert.Equal("เริ่มฝึกซ้อม", result.TodaySessions[0].RequiredNextAction);

        // Only the Scheduled future session is "upcoming" — the Cancelled one is excluded.
        Assert.Single(result.UpcomingSessions);
        Assert.Equal(today.AddDays(2), result.UpcomingSessions[0].SessionDate);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesMonthlyCountsAndTeachingHours()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var now = new DateTime(2026, 1, 20, 9, 0, 0);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 5), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Completed,
                actualStart: new DateTime(2026, 1, 5, 17, 0, 0), actualEnd: new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 6), new TimeOnly(17, 0), new TimeOnly(18, 30), SessionStatus.Completed,
                type: TrainingType.Private, actualStart: new DateTime(2026, 1, 6, 17, 0, 0), actualEnd: new DateTime(2026, 1, 6, 18, 30, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 25), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Scheduled),
            BuildSession(coach, new DateOnly(2025, 12, 30), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Completed,
                actualStart: new DateTime(2025, 12, 30, 17, 0, 0), actualEnd: new DateTime(2025, 12, 30, 19, 0, 0))); // previous month — excluded
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(coach.CoachId, now);

        Assert.Equal(2, result.CompletedSessionCount);
        Assert.Equal(1, result.RemainingSessionCount);
        Assert.Equal(2, result.RoutineSessionCount);
        Assert.Equal(1, result.PrivateSessionCount);
        Assert.Equal(3.5m, result.MonthlyTeachingHours);
    }

    [Fact]
    public async Task GetDashboardAsync_CountsPendingActionForOverdueAndUnsubmittedSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var now = new DateTime(2026, 1, 10, 12, 0, 0);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 10), new TimeOnly(8, 0), new TimeOnly(9, 0), SessionStatus.Scheduled), // overdue, needs start
            BuildSession(coach, new DateOnly(2026, 1, 10), new TimeOnly(18, 0), new TimeOnly(19, 0), SessionStatus.Scheduled), // still in the future
            BuildSession(coach, new DateOnly(2026, 1, 9), new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Completed)); // needs submit
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(coach.CoachId, now);

        Assert.Equal(2, result.PendingActionCount);
    }

    [Fact]
    public async Task GetDashboardAsync_NeverIncludesAnotherCoachsSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var now = new DateTime(2026, 1, 10, 9, 0, 0);
        var today = DateOnly.FromDateTime(now);

        db.TrainingSessions.Add(BuildSession(otherCoach, today, new TimeOnly(17, 0), new TimeOnly(19, 0), SessionStatus.Scheduled));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(coach.CoachId, now);

        Assert.Empty(result.TodaySessions);
    }
}
