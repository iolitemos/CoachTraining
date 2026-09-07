using CoachTraining.Api.DTOs.PrivateAttendance;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class PrivateAttendanceServiceTests
{
    private static PrivateAttendanceService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<PrivateAttendanceService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<Athlete> SeedAthleteAsync(Data.ApplicationDbContext db, string code)
    {
        var athlete = new Athlete { AthleteCode = code, FullName = $"Athlete {code}", IsActive = true };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync();
        return athlete;
    }

    private static async Task<TrainingSession> SeedPrivateSessionAsync(
        Data.ApplicationDbContext db, Coach coach, IEnumerable<Athlete> athletes, SessionStatus status = SessionStatus.InProgress)
    {
        var date = new DateOnly(2026, 1, 5);
        var session = new TrainingSession
        {
            TrainingType = TrainingType.Private,
            SessionDate = date,
            ScheduledStartDateTime = date.ToDateTime(new TimeOnly(17, 0)),
            ScheduledEndDateTime = date.ToDateTime(new TimeOnly(18, 0)),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = status,
        };
        foreach (var athlete in athletes)
        {
            session.PrivateAthletes.Add(new PrivateSessionAthlete
            {
                AthleteId = athlete.AthleteId,
                AthleteCodeSnapshot = athlete.AthleteCode,
                AthleteNameSnapshot = athlete.FullName,
            });
        }
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task GetRosterAsync_ListsAllAssignedAthletesRegardlessOfRecordedAttendance()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var session = await SeedPrivateSessionAsync(db, coach, [athleteA, athleteB]);
        var service = CreateService(db);

        var roster = await service.GetRosterAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId);

        Assert.NotNull(roster);
        Assert.Equal(2, roster!.Athletes.Count);
        Assert.All(roster.Athletes, a => Assert.Null(a.Status));
        Assert.False(roster.IsComplete);
    }

    [Fact]
    public async Task GetRosterAsync_AsUnauthorizedCoach_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, assignedCoach, [athlete]);
        var service = CreateService(db);

        var roster = await service.GetRosterAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: otherCoach.CoachId);

        Assert.Null(roster);
    }

    [Fact]
    public async Task GetRosterAsync_OnRoutineSession_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
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
            Status = SessionStatus.InProgress,
        };
        db.TrainingSessions.Add(session);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var roster = await service.GetRosterAsync(session.TrainingSessionId, isPrivilegedRole: true, currentCoachId: null);

        Assert.Null(roster);
    }

    [Fact]
    public async Task SetAsync_ForAssignedAthlete_CreatesAttendance()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, coach, [athlete]);
        var service = CreateService(db);

        var result = await service.SetAsync(
            session.TrainingSessionId, athlete.AthleteId,
            new PrivateAttendanceSetRequest { Status = AttendanceStatus.Present },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.NotNull(result.Attendance);
        Assert.Equal(AttendanceStatus.Present, result.Attendance!.Status);
        Assert.True(result.RosterComplete);
    }

    [Fact]
    public async Task SetAsync_ForUnassignedAthlete_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var assignedAthlete = await SeedAthleteAsync(db, "A001");
        var unassignedAthlete = await SeedAthleteAsync(db, "A002");
        var session = await SeedPrivateSessionAsync(db, coach, [assignedAthlete]);
        var service = CreateService(db);

        var result = await service.SetAsync(
            session.TrainingSessionId, unassignedAthlete.AthleteId,
            new PrivateAttendanceSetRequest { Status = AttendanceStatus.Present },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.NotNull(result.Error);
        Assert.Null(result.Attendance);
    }

    [Theory]
    [InlineData(AttendanceStatus.Present)]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Late)]
    [InlineData(AttendanceStatus.Excused)]
    public async Task SetAsync_AllowsAllFourStatuses(AttendanceStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, coach, [athlete]);
        var service = CreateService(db);

        var result = await service.SetAsync(
            session.TrainingSessionId, athlete.AthleteId, new PrivateAttendanceSetRequest { Status = status },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.Null(result.Error);
        Assert.Equal(status, result.Attendance!.Status);
    }

    [Fact]
    public async Task SetAsync_CalledTwice_UpdatesExistingRatherThanDuplicating()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, coach, [athlete]);
        var service = CreateService(db);

        await service.SetAsync(session.TrainingSessionId, athlete.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Absent, Remark = "ป่วย" }, false, coach.CoachId, 1);
        var updated = await service.SetAsync(session.TrainingSessionId, athlete.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Late, ArrivalTime = new TimeOnly(17, 10) }, false, coach.CoachId, 1);

        Assert.Equal(AttendanceStatus.Late, updated.Attendance!.Status);
        Assert.Equal(new TimeOnly(17, 10), updated.Attendance.ArrivalTime);

        var allAttendance = await db.Attendances.Where(a => a.TrainingSessionId == session.TrainingSessionId).ToListAsync();
        Assert.Single(allAttendance);
    }

    [Fact]
    public async Task SetAsync_RosterCompleteBecomesTrueOnlyAfterLastAthleteRecorded()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athleteA = await SeedAthleteAsync(db, "A001");
        var athleteB = await SeedAthleteAsync(db, "A002");
        var session = await SeedPrivateSessionAsync(db, coach, [athleteA, athleteB]);
        var service = CreateService(db);

        var first = await service.SetAsync(session.TrainingSessionId, athleteA.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Present }, false, coach.CoachId, 1);
        Assert.False(first.RosterComplete);

        var second = await service.SetAsync(session.TrainingSessionId, athleteB.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Absent }, false, coach.CoachId, 1);
        Assert.True(second.RosterComplete);
    }

    [Fact]
    public async Task SetAsync_OnLockedSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, coach, [athlete], SessionStatus.Locked);
        var service = CreateService(db);

        var result = await service.SetAsync(session.TrainingSessionId, athlete.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SetAsync_ByAnotherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var athlete = await SeedAthleteAsync(db, "A001");
        var session = await SeedPrivateSessionAsync(db, assignedCoach, [athlete]);
        var service = CreateService(db);

        var result = await service.SetAsync(session.TrainingSessionId, athlete.AthleteId, new PrivateAttendanceSetRequest { Status = AttendanceStatus.Present }, false, otherCoach.CoachId, 1);

        Assert.True(result.Forbidden);
    }
}
