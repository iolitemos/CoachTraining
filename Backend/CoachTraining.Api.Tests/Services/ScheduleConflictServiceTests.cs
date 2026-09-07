using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class ScheduleConflictServiceTests
{
    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static TrainingSession BuildSession(Coach coach, DateOnly date, TimeOnly start, TimeOnly end, SessionStatus status = SessionStatus.Scheduled) => new()
    {
        TrainingType = TrainingType.Routine,
        SessionDate = date,
        ScheduledStartDateTime = date.ToDateTime(start),
        ScheduledEndDateTime = date.ToDateTime(end),
        AssignedCoachId = coach.CoachId,
        AssignedCoachCodeSnapshot = coach.CoachCode,
        AssignedCoachNameSnapshot = coach.FullName,
        Status = status,
    };

    private static async Task<Athlete> SeedAthleteAsync(Data.ApplicationDbContext db, string code = "A001")
    {
        var athlete = new Athlete { AthleteCode = code, FullName = $"Athlete {code}", IsActive = true };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync();
        return athlete;
    }

    private static TrainingSession BuildPrivateSession(Coach coach, Athlete athlete, DateOnly date, TimeOnly start, TimeOnly end, SessionStatus status = SessionStatus.Scheduled)
    {
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Private,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(start),
            ScheduledEndDateTime = date.ToDateTime(end),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = status,
        };
        session.PrivateAthletes.Add(new PrivateSessionAthlete
        {
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
        });
        return session;
    }

    [Fact]
    public async Task CheckCoachOverlapAsync_WithOverlappingSession_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildSession(coach, date, new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckCoachOverlapAsync(coach.CoachId, date.ToDateTime(new TimeOnly(18, 0)), date.ToDateTime(new TimeOnly(20, 0)));

        Assert.Single(conflicts);
    }

    [Fact]
    public async Task CheckCoachOverlapAsync_WithAdjacentNonOverlappingSession_ReturnsNoConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildSession(coach, date, new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        // Starts exactly when the other ends — back-to-back, not overlapping.
        var conflicts = await service.CheckCoachOverlapAsync(coach.CoachId, date.ToDateTime(new TimeOnly(19, 0)), date.ToDateTime(new TimeOnly(20, 0)));

        Assert.Empty(conflicts);
    }

    [Theory]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Rescheduled)]
    public async Task CheckCoachOverlapAsync_IgnoresNonOccupyingStatuses(SessionStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildSession(coach, date, new TimeOnly(17, 0), new TimeOnly(19, 0), status));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckCoachOverlapAsync(coach.CoachId, date.ToDateTime(new TimeOnly(17, 30)), date.ToDateTime(new TimeOnly(18, 30)));

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckCoachOverlapAsync_ExcludesGivenSessionId()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var date = new DateOnly(2026, 1, 5);
        var session = BuildSession(coach, date, new TimeOnly(17, 0), new TimeOnly(19, 0));
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckCoachOverlapAsync(
            coach.CoachId, date.ToDateTime(new TimeOnly(17, 0)), date.ToDateTime(new TimeOnly(19, 0)),
            excludeTrainingSessionId: session.TrainingSessionId);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckRoutineTemplateOverlapAsync_WithSameDayOverlappingTimeAndDateRange_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        db.RoutineSchedules.Add(new RoutineSchedule
        {
            Name = "Existing", CoachId = coach.CoachId, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = new DateOnly(2026, 1, 1), IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckRoutineTemplateOverlapAsync(
            coach.CoachId, DayOfWeek.Monday, new TimeOnly(18, 0), new TimeOnly(20, 0), new DateOnly(2026, 2, 1), null);

        Assert.Single(conflicts);
    }

    [Fact]
    public async Task CheckRoutineTemplateOverlapAsync_WithDifferentDayOfWeek_ReturnsNoConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        db.RoutineSchedules.Add(new RoutineSchedule
        {
            Name = "Existing", CoachId = coach.CoachId, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = new DateOnly(2026, 1, 1), IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckRoutineTemplateOverlapAsync(
            coach.CoachId, DayOfWeek.Tuesday, new TimeOnly(17, 0), new TimeOnly(19, 0), new DateOnly(2026, 1, 1), null);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckRoutineTemplateOverlapAsync_ExcludesGivenScheduleId()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var schedule = new RoutineSchedule
        {
            Name = "Existing", CoachId = coach.CoachId, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = new DateOnly(2026, 1, 1), IsActive = true,
        };
        db.RoutineSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckRoutineTemplateOverlapAsync(
            coach.CoachId, DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(19, 0), new DateOnly(2026, 1, 1), null,
            excludeRoutineScheduleId: schedule.RoutineScheduleId);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckRoutineTemplateOverlapAsync_WithNonOverlappingEffectiveDateRanges_ReturnsNoConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        db.RoutineSchedules.Add(new RoutineSchedule
        {
            Name = "Existing", CoachId = coach.CoachId, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(19, 0),
            EffectiveStartDate = new DateOnly(2026, 1, 1), EffectiveEndDate = new DateOnly(2026, 3, 1), IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckRoutineTemplateOverlapAsync(
            coach.CoachId, DayOfWeek.Monday, new TimeOnly(17, 0), new TimeOnly(19, 0), new DateOnly(2026, 4, 1), null);

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckAthleteOverlapAsync_WithOverlappingPrivateSession_ReturnsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildPrivateSession(coach, athlete, date, new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckAthleteOverlapAsync(
            [athlete.AthleteId], date.ToDateTime(new TimeOnly(18, 0)), date.ToDateTime(new TimeOnly(20, 0)));

        var conflict = Assert.Single(conflicts);
        Assert.Equal(athlete.AthleteId, conflict.AthleteId);
    }

    [Fact]
    public async Task CheckAthleteOverlapAsync_WithDifferentAthlete_ReturnsNoConflict()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildPrivateSession(coach, athleteA, date, new TimeOnly(17, 0), new TimeOnly(19, 0)));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckAthleteOverlapAsync(
            [athleteB.AthleteId], date.ToDateTime(new TimeOnly(17, 0)), date.ToDateTime(new TimeOnly(19, 0)));

        Assert.Empty(conflicts);
    }

    [Theory]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.Rescheduled)]
    public async Task CheckAthleteOverlapAsync_IgnoresNonOccupyingStatuses(SessionStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var date = new DateOnly(2026, 1, 5);
        db.TrainingSessions.Add(BuildPrivateSession(coach, athlete, date, new TimeOnly(17, 0), new TimeOnly(19, 0), status));
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckAthleteOverlapAsync(
            [athlete.AthleteId], date.ToDateTime(new TimeOnly(17, 30)), date.ToDateTime(new TimeOnly(18, 30)));

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task CheckAthleteOverlapAsync_ExcludesGivenSessionId()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var date = new DateOnly(2026, 1, 5);
        var session = BuildPrivateSession(coach, athlete, date, new TimeOnly(17, 0), new TimeOnly(19, 0));
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();

        var service = new ScheduleConflictService(db);
        var conflicts = await service.CheckAthleteOverlapAsync(
            [athlete.AthleteId], date.ToDateTime(new TimeOnly(17, 0)), date.ToDateTime(new TimeOnly(19, 0)),
            excludeTrainingSessionId: session.TrainingSessionId);

        Assert.Empty(conflicts);
    }
}
