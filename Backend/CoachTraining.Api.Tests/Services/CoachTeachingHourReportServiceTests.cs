using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CoachTeachingHourReportServiceTests
{
    private static CoachTeachingHourReportService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService());

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach
        {
            CoachCode = code,
            FullName = $"Coach {code}",
            Nickname = $"Nick {code}",
            ColorHex = "#10B981",
            IsActive = true,
        };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static TrainingSession BuildSession(
        Coach coach, DateOnly date, SessionStatus status, TrainingType type,
        DateTime? actualStart, DateTime? actualEnd, Coach? actualCoach = null) => new()
    {
        TrainingType = type,
        SessionDate = date,
        ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
        ScheduledEndDateTime = date.ToDateTime(new TimeOnly(19, 0)),
        ActualStartDateTime = actualStart,
        ActualEndDateTime = actualEnd,
        AssignedCoachId = coach.CoachId,
        AssignedCoachCodeSnapshot = coach.CoachCode,
        AssignedCoachNameSnapshot = coach.FullName,
        ActualCoachId = actualCoach?.CoachId,
        ActualCoachCodeSnapshot = actualCoach?.CoachCode,
        ActualCoachNameSnapshot = actualCoach?.FullName,
        Status = status,
    };

    [Fact]
    public async Task GetReportAsync_CountsDistinctTeachingDaysAcrossSessionsAndTypes()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Completed, TrainingType.Routine,
                new DateTime(2026, 1, 5, 17, 0, 0), new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Locked, TrainingType.Routine,
                new DateTime(2026, 1, 5, 19, 0, 0), new DateTime(2026, 1, 5, 20, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Locked, TrainingType.Private,
                new DateTime(2026, 1, 5, 20, 0, 0), new DateTime(2026, 1, 5, 21, 30, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new CoachTeachingHourReportFilter());

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.RoutineDays);
        Assert.Equal(1, item.PrivateDays);
        Assert.Equal(1, item.TotalDays);
        Assert.Equal(3, item.SessionCount);
        Assert.Equal("Nick C001", item.CoachNickname);
        Assert.Equal("#10B981", item.CoachColorHex);
        Assert.Equal(1, result.TotalRoutineDays);
        Assert.Equal(1, result.TotalPrivateDays);
        Assert.Equal(1, result.GrandTotalDays);
    }

    [Fact]
    public async Task GetReportAsync_ExcludesCancelledRescheduledAndCoachAbsentSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Cancelled, TrainingType.Routine, new DateTime(2026, 1, 5, 17, 0, 0), new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 6), SessionStatus.Rescheduled, TrainingType.Routine, new DateTime(2026, 1, 6, 17, 0, 0), new DateTime(2026, 1, 6, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 7), SessionStatus.CoachAbsent, TrainingType.Routine, null, null),
            BuildSession(coach, new DateOnly(2026, 1, 8), SessionStatus.Scheduled, TrainingType.Routine, null, null));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new CoachTeachingHourReportFilter());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.GrandTotalDays);
    }

    [Fact]
    public async Task GetReportAsync_CreditsActualCoachAfterSubstitution()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");

        db.TrainingSessions.Add(BuildSession(
            assignedCoach, new DateOnly(2026, 1, 5), SessionStatus.Completed, TrainingType.Routine,
            new DateTime(2026, 1, 5, 17, 0, 0), new DateTime(2026, 1, 5, 19, 0, 0), actualCoach: substituteCoach));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new CoachTeachingHourReportFilter());

        var item = Assert.Single(result.Items);
        Assert.Equal(substituteCoach.CoachId, item.CoachId);
        Assert.Equal(1, item.TotalDays);
    }

    [Fact]
    public async Task GetReportAsync_FiltersByCoachDateRangeAndTrainingType()
    {
        using var db = TestDbContextFactory.Create();
        var coachA = await SeedCoachAsync(db, "C001");
        var coachB = await SeedCoachAsync(db, "C002");

        db.TrainingSessions.AddRange(
            BuildSession(coachA, new DateOnly(2026, 1, 5), SessionStatus.Completed, TrainingType.Routine, new DateTime(2026, 1, 5, 17, 0, 0), new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coachB, new DateOnly(2026, 1, 5), SessionStatus.Completed, TrainingType.Routine, new DateTime(2026, 1, 5, 17, 0, 0), new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coachA, new DateOnly(2026, 2, 5), SessionStatus.Completed, TrainingType.Routine, new DateTime(2026, 2, 5, 17, 0, 0), new DateTime(2026, 2, 5, 19, 0, 0)),
            BuildSession(coachA, new DateOnly(2026, 1, 6), SessionStatus.Completed, TrainingType.Private, new DateTime(2026, 1, 6, 17, 0, 0), new DateTime(2026, 1, 6, 18, 0, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new CoachTeachingHourReportFilter
        {
            CoachId = coachA.CoachId,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            TrainingType = TrainingType.Routine,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(coachA.CoachId, item.CoachId);
        Assert.Equal(1, item.RoutineDays);
        Assert.Equal(0, item.PrivateDays);
    }
}
