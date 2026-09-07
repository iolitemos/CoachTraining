using CoachTraining.Api.DTOs.RoutineAttendance;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class RoutineAttendanceServiceTests
{
    private static RoutineAttendanceService CreateService(Data.ApplicationDbContext db) =>
        new(db, new SessionStatusService(), NullLogger<RoutineAttendanceService>.Instance);

    private static async Task<Coach> SeedCoachAsync(Data.ApplicationDbContext db, string code = "C001")
    {
        var coach = new Coach { CoachCode = code, FullName = $"Coach {code}", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        return coach;
    }

    private static async Task<Athlete> SeedAthleteAsync(Data.ApplicationDbContext db, string code = "A001", bool isActive = true)
    {
        var athlete = new Athlete { AthleteCode = code, FullName = $"Athlete {code}", IsActive = isActive };
        db.Athletes.Add(athlete);
        await db.SaveChangesAsync();
        return athlete;
    }

    private static async Task<TrainingSession> SeedSessionAsync(
        Data.ApplicationDbContext db, Coach coach, SessionStatus status = SessionStatus.InProgress, TrainingType type = TrainingType.Routine)
    {
        var date = new DateOnly(2026, 1, 5);
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
    public async Task AddAsync_WithValidPresentStatus_CreatesAttendance()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.AddAsync(
            session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.True(result.Success);
        Assert.NotNull(result.Attendance);
        Assert.Equal(AttendanceStatus.Present, result.Attendance!.Status);
        Assert.Equal(athlete.AthleteCode, result.Attendance.AthleteCode);
    }

    [Theory]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Excused)]
    public async Task AddAsync_WithDisallowedStatus_ReturnsError(AttendanceStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.AddAsync(
            session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = status },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AddAsync_WithLateStatus_StoresArrivalTime()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.AddAsync(
            session.TrainingSessionId,
            new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Late, ArrivalTime = new TimeOnly(17, 20) },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.True(result.Success);
        Assert.Equal(new TimeOnly(17, 20), result.Attendance!.ArrivalTime);
    }

    [Fact]
    public async Task AddAsync_WithPresentStatus_IgnoresSuppliedArrivalTime()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.AddAsync(
            session.TrainingSessionId,
            new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present, ArrivalTime = new TimeOnly(17, 20) },
            isPrivilegedRole: false, currentCoachId: coach.CoachId, actionByUserId: 1);

        Assert.Null(result.Attendance!.ArrivalTime);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateAthlete_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);
        var second = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Late }, false, coach.CoachId, 1);

        Assert.False(second.Success);
        Assert.NotNull(second.Error);
    }

    [Fact]
    public async Task AddAsync_WithInactiveAthlete_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db, isActive: false);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AddAsync_OnPrivateSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach, type: TrainingType.Private);
        var service = CreateService(db);

        var result = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AddAsync_OnLockedSession_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach, status: SessionStatus.Locked);
        var service = CreateService(db);

        var result = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AddAsync_ByAnotherCoach_ReturnsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, assignedCoach);
        var service = CreateService(db);

        var result = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, otherCoach.CoachId, 1);

        Assert.True(result.Forbidden);
    }

    [Fact]
    public async Task ListAsync_OnlyReturnsSelectedAthletes_NeverInfersAbsenceForOthers()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var attendedAthlete = await SeedAthleteAsync(db, "A001");
        await SeedAthleteAsync(db, "A002"); // never selected — must not appear as Absent
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = attendedAthlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        var list = await service.ListAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: coach.CoachId);

        var item = Assert.Single(list!);
        Assert.Equal(attendedAthlete.AthleteCode, item.AthleteCode);
    }

    [Fact]
    public async Task ListAsync_AsUnauthorizedCoach_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var assignedCoach = await SeedCoachAsync(db, "C001");
        var otherCoach = await SeedCoachAsync(db, "C002");
        var session = await SeedSessionAsync(db, assignedCoach);
        var service = CreateService(db);

        var list = await service.ListAsync(session.TrainingSessionId, isPrivilegedRole: false, currentCoachId: otherCoach.CoachId);

        Assert.Null(list);
    }

    [Fact]
    public async Task UpdateAsync_ChangesStatusFromLateToPresent()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);
        var created = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Late, ArrivalTime = new TimeOnly(17, 15) }, false, coach.CoachId, 1);

        var updated = await service.UpdateAsync(session.TrainingSessionId, created.Attendance!.AttendanceId, new RoutineAttendanceUpdateDto { Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        Assert.True(updated.Success);
        Assert.Equal(AttendanceStatus.Present, updated.Attendance!.Status);
        Assert.Null(updated.Attendance.ArrivalTime);
    }

    [Fact]
    public async Task RemoveAsync_DeletesAttendanceEntry()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var athlete = await SeedAthleteAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);
        var created = await service.AddAsync(session.TrainingSessionId, new RoutineAttendanceCreateDto { AthleteId = athlete.AthleteId, Status = AttendanceStatus.Present }, false, coach.CoachId, 1);

        var removed = await service.RemoveAsync(session.TrainingSessionId, created.Attendance!.AttendanceId, false, coach.CoachId);
        var list = await service.ListAsync(session.TrainingSessionId, false, coach.CoachId);

        Assert.True(removed.Success);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task RemoveAsync_WithUnknownAttendanceId_ReturnsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var coach = await SeedCoachAsync(db);
        var session = await SeedSessionAsync(db, coach);
        var service = CreateService(db);

        var result = await service.RemoveAsync(session.TrainingSessionId, 999, false, coach.CoachId);

        Assert.True(result.NotFound);
    }
}
