using CoachTraining.Api.DTOs.Dashboards;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AdministratorDashboardServiceTests
{
    private static AdministratorDashboardService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService());

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

    private static TrainingSession BuildSession(
        Coach coach, DateOnly date, SessionStatus status, TrainingType type = TrainingType.Routine,
        DateTime? actualStart = null, DateTime? actualEnd = null) => new()
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
        Status = status,
    };

    [Fact]
    public async Task GetDashboardAsync_WithNoFilter_DefaultsRangeToToday()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var now = new DateTime(2026, 1, 10, 9, 0, 0);
        var today = DateOnly.FromDateTime(now);

        db.TrainingSessions.AddRange(
            BuildSession(coach, today, SessionStatus.Scheduled),
            BuildSession(coach, today.AddDays(1), SessionStatus.Scheduled)); // outside the default today-only range
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(new AdministratorDashboardFilterRequest(), now);

        Assert.Equal(1, result.SessionsTodayCount);
        Assert.Equal(1, result.UpcomingCount); // today's own Scheduled session also counts as "upcoming" within [today, today]
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesCountsByStatusAndType()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var now = new DateTime(2026, 1, 10, 9, 0, 0);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Completed, actualStart: new DateTime(2026, 1, 5, 17, 0, 0), actualEnd: new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 6), SessionStatus.Cancelled, type: TrainingType.Private),
            BuildSession(coach, new DateOnly(2026, 1, 7), SessionStatus.Scheduled));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var filter = new AdministratorDashboardFilterRequest { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        var result = await service.GetDashboardAsync(filter, now);

        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(1, result.CancelledCount);
        Assert.Equal(2, result.RoutineCount);
        Assert.Equal(1, result.PrivateCount);
    }

    [Fact]
    public async Task GetDashboardAsync_GroupsCoachesTeachingTodayByCreditedCoach()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var substituteCoach = await SeedCoachAsync(db, "C002");
        var now = new DateTime(2026, 1, 10, 9, 0, 0);
        var today = DateOnly.FromDateTime(now);

        var substituted = BuildSession(assignedCoach, today, SessionStatus.Scheduled);
        substituted.ActualCoachId = substituteCoach.CoachId;
        substituted.ActualCoachCodeSnapshot = substituteCoach.CoachCode;
        substituted.ActualCoachNameSnapshot = substituteCoach.FullName;

        db.TrainingSessions.AddRange(substituted, BuildSession(assignedCoach, today, SessionStatus.Scheduled));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetDashboardAsync(new AdministratorDashboardFilterRequest(), now);

        var substituteEntry = Assert.Single(result.CoachesTeachingToday, c => c.CoachId == substituteCoach.CoachId);
        Assert.Equal(1, substituteEntry.SessionCount);
        var assignedEntry = Assert.Single(result.CoachesTeachingToday, c => c.CoachId == assignedCoach.CoachId);
        Assert.Equal(1, assignedEntry.SessionCount);
    }

    [Fact]
    public async Task GetDashboardAsync_AggregatesAttendanceSummaryAcrossRange()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Completed, type: TrainingType.Private);
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        db.Attendances.AddRange(
            new Attendance { TrainingSessionId = session.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Present },
            new Attendance { TrainingSessionId = session.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Late });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var filter = new AdministratorDashboardFilterRequest { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        var result = await service.GetDashboardAsync(filter, new DateTime(2026, 1, 10, 9, 0, 0));

        Assert.Equal(1, result.AttendanceSummary.PresentCount);
        Assert.Equal(1, result.AttendanceSummary.LateCount);
    }

    [Fact]
    public async Task GetDashboardAsync_ComputesCoachTeachingHoursSplitByType()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);

        db.TrainingSessions.AddRange(
            BuildSession(coach, new DateOnly(2026, 1, 5), SessionStatus.Completed, actualStart: new DateTime(2026, 1, 5, 17, 0, 0), actualEnd: new DateTime(2026, 1, 5, 19, 0, 0)),
            BuildSession(coach, new DateOnly(2026, 1, 6), SessionStatus.Completed, type: TrainingType.Private, actualStart: new DateTime(2026, 1, 6, 17, 0, 0), actualEnd: new DateTime(2026, 1, 6, 18, 0, 0)));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var filter = new AdministratorDashboardFilterRequest { StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 1, 31) };
        var result = await service.GetDashboardAsync(filter, new DateTime(2026, 1, 10, 9, 0, 0));

        var entry = Assert.Single(result.CoachTeachingHours);
        Assert.Equal(2m, entry.RoutineHours);
        Assert.Equal(1m, entry.PrivateHours);
        Assert.Equal(3m, entry.TotalHours);
    }
}
