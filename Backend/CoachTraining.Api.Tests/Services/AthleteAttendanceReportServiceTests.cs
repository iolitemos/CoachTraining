using CoachTraining.Api.DTOs.Reports;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AthleteAttendanceReportServiceTests
{
    private static AthleteAttendanceReportService CreateService(Data.ApplicationDbContext db) => new(db);

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
        Data.ApplicationDbContext db, Coach coach, DateOnly date, TrainingType type, SessionStatus status = SessionStatus.Completed)
    {
        var session = new TrainingSession
        {
            TrainingType = type,
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
    public async Task GetReportAsync_SplitsCountsByTrainingTypeAndStatus()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var routineSession = await SeedSessionAsync(db, coach, new DateOnly(2026, 1, 5), TrainingType.Routine);
        var privateSession = await SeedSessionAsync(db, coach, new DateOnly(2026, 1, 6), TrainingType.Private);

        db.Attendances.AddRange(
            new Attendance { TrainingSessionId = routineSession.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Present },
            new Attendance { TrainingSessionId = privateSession.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Absent });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new AthleteAttendanceReportFilter());

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.RoutinePresentCount);
        Assert.Equal(0, item.RoutineLateCount);
        Assert.Equal(1, item.PrivateAbsentCount);
        Assert.Equal(2, item.Records.Count);
    }

    [Fact]
    public async Task GetReportAsync_ExcludesRescheduledOriginalSessions()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var rescheduledOriginal = await SeedSessionAsync(db, coach, new DateOnly(2026, 1, 5), TrainingType.Routine, SessionStatus.Rescheduled);

        db.Attendances.Add(new Attendance
        {
            TrainingSessionId = rescheduledOriginal.TrainingSessionId,
            AthleteId = athlete.AthleteId,
            AthleteCodeSnapshot = athlete.AthleteCode,
            AthleteNameSnapshot = athlete.FullName,
            Status = AttendanceStatus.Present,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new AthleteAttendanceReportFilter());

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetReportAsync_FiltersByAthleteAndDateRange()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var sessionInRange = await SeedSessionAsync(db, coach, new DateOnly(2026, 1, 5), TrainingType.Routine);
        var sessionOutOfRange = await SeedSessionAsync(db, coach, new DateOnly(2026, 2, 5), TrainingType.Routine);

        db.Attendances.AddRange(
            new Attendance { TrainingSessionId = sessionInRange.TrainingSessionId, AthleteId = athleteA.AthleteId, AthleteCodeSnapshot = athleteA.AthleteCode, AthleteNameSnapshot = athleteA.FullName, Status = AttendanceStatus.Present },
            new Attendance { TrainingSessionId = sessionInRange.TrainingSessionId, AthleteId = athleteB.AthleteId, AthleteCodeSnapshot = athleteB.AthleteCode, AthleteNameSnapshot = athleteB.FullName, Status = AttendanceStatus.Present },
            new Attendance { TrainingSessionId = sessionOutOfRange.TrainingSessionId, AthleteId = athleteA.AthleteId, AthleteCodeSnapshot = athleteA.AthleteCode, AthleteNameSnapshot = athleteA.FullName, Status = AttendanceStatus.Present });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetReportAsync(new AthleteAttendanceReportFilter
        {
            AthleteId = athleteA.AthleteId,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(athleteA.AthleteId, item.AthleteId);
        Assert.Single(item.Records);
    }
}
