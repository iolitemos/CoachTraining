using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoachTraining.Api.Tests.Services;

public class PublicRoutineCalendarServiceTests
{
    [Fact]
    public async Task RotateLinkAsync_InvalidatesPreviousLink()
    {
        using var db = TestDbContextFactory.Create();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);

        var first = await service.RotateLinkAsync(7);
        var second = await service.RotateLinkAsync(7);

        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(2, db.RoutineCalendarShareLinks.Count());
        Assert.Single(db.RoutineCalendarShareLinks.Where(link => link.RevokedAtUtc == null));
        Assert.Null(await service.GetCalendarAsync(first.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));
        Assert.NotNull(await service.GetCalendarAsync(second.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsOnlyActiveSchedulesInRequestedRange()
    {
        using var db = TestDbContextFactory.Create();
        var coach = new Coach { CoachCode = "C001", FullName = "Private Full Name", Nickname = "ปิง", ColorHex = "#123456", IsActive = true };
        db.Coaches.Add(coach);
        await db.SaveChangesAsync();
        db.RoutineSchedules.AddRange(
            new RoutineSchedule { CoachId = coach.CoachId, EffectiveStartDate = new DateOnly(2026, 9, 12), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsActive = true, CreatedDate = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc), UpdatedDate = new DateTime(2026, 9, 10, 4, 30, 0, DateTimeKind.Utc) },
            new RoutineSchedule { CoachId = coach.CoachId, EffectiveStartDate = new DateOnly(2026, 9, 13), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsActive = false });
        await db.SaveChangesAsync();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var item = Assert.Single(result!.Schedules);
        Assert.Equal("C001", item.CoachCode);
        Assert.Equal("ปิง", item.CoachNickname);
        Assert.Equal("#123456", item.CoachColorHex);
        Assert.Equal(new DateOnly(2026, 9, 12), item.TrainingDate);
        Assert.Equal(new DateTime(2026, 9, 10, 4, 30, 0, DateTimeKind.Utc), item.LatestUpdate);
    }

    [Fact]
    public async Task GetCalendarAsync_SortsSchedulesByCoachCodeAndThenStartTime()
    {
        using var db = TestDbContextFactory.Create();
        var firstCoach = new Coach { CoachCode = "C001", FullName = "Coach 1", Nickname = "หนึ่ง", ColorHex = "#123456", IsActive = true };
        var secondCoach = new Coach { CoachCode = "C002", FullName = "Coach 2", Nickname = "สอง", ColorHex = "#654321", IsActive = true };
        db.Coaches.AddRange(firstCoach, secondCoach);
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 9, 12);
        db.RoutineSchedules.AddRange(
            new RoutineSchedule { CoachId = secondCoach.CoachId, EffectiveStartDate = date, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsActive = true },
            new RoutineSchedule { CoachId = firstCoach.CoachId, EffectiveStartDate = date, StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(18, 0), IsActive = true },
            new RoutineSchedule { CoachId = secondCoach.CoachId, EffectiveStartDate = date, StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(14, 0), IsActive = true });
        await db.SaveChangesAsync();

        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, date, date);

        Assert.Collection(
            result!.Schedules,
            item => { Assert.Equal("C001", item.CoachCode); Assert.Equal(new TimeOnly(17, 0), item.StartTime); },
            item => { Assert.Equal("C002", item.CoachCode); Assert.Equal(new TimeOnly(9, 0), item.StartTime); },
            item => { Assert.Equal("C002", item.CoachCode); Assert.Equal(new TimeOnly(13, 0), item.StartTime); });
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsCompetitionMatchesOverlappingRequestedRange()
    {
        using var db = TestDbContextFactory.Create();
        db.CompetitionMatches.Add(new CompetitionMatch
        {
            Name = "ชิงแชมป์ประเทศไทย",
            Province = "กรุงเทพมหานคร",
            StartDate = new DateOnly(2026, 9, 10),
            EndDate = new DateOnly(2026, 9, 12),
        });
        await db.SaveChangesAsync();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 9, 11), new DateOnly(2026, 9, 30));

        var match = Assert.Single(result!.CompetitionMatches);
        Assert.Equal("ชิงแชมป์ประเทศไทย", match.Name);
        Assert.Equal(new DateOnly(2026, 9, 10), match.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 12), match.EndDate);
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsLatestUpdateForCalendarNote()
    {
        using var db = TestDbContextFactory.Create();
        db.CalendarNotes.Add(new CalendarNote
        {
            NoteDate = new DateOnly(2026, 9, 12),
            Content = "เปลี่ยนเวลาฝึกซ้อม",
            CreatedDate = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc),
            UpdatedDate = new DateTime(2026, 9, 11, 5, 45, 0, DateTimeKind.Utc),
        });
        await db.SaveChangesAsync();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var note = Assert.Single(result!.Notes);
        Assert.Equal(new DateTime(2026, 9, 11, 5, 45, 0, DateTimeKind.Utc), note.LatestUpdate);
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsRoutineAttendanceSummaryOnlyForPresentAndLateRecords()
    {
        using var db = TestDbContextFactory.Create();
        var coach = new Coach { CoachCode = "C001", FullName = "Coach", Nickname = "โค้ช", ColorHex = "#123456", IsActive = true };
        var athlete = new Athlete { AthleteCode = "A001", FullName = "Athlete", Nickname = "นักกีฬา", IsActive = true };
        db.AddRange(coach, athlete);
        await db.SaveChangesAsync();

        var sessionDate = new DateOnly(2026, 9, 12);
        var firstSession = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = sessionDate,
            ScheduledStartDateTime = new DateTime(2026, 9, 12, 9, 0, 0),
            ScheduledEndDateTime = new DateTime(2026, 9, 12, 10, 0, 0),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = SessionStatus.Completed,
        };
        var secondSession = new TrainingSession
        {
            TrainingType = TrainingType.Routine,
            SessionDate = sessionDate.AddDays(1),
            ScheduledStartDateTime = new DateTime(2026, 9, 13, 9, 0, 0),
            ScheduledEndDateTime = new DateTime(2026, 9, 13, 10, 0, 0),
            AssignedCoachId = coach.CoachId,
            AssignedCoachCodeSnapshot = coach.CoachCode,
            AssignedCoachNameSnapshot = coach.FullName,
            Status = SessionStatus.Completed,
        };
        db.TrainingSessions.AddRange(firstSession, secondSession);
        await db.SaveChangesAsync();
        db.Attendances.AddRange(
            new Attendance { TrainingSessionId = firstSession.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Present },
            new Attendance { TrainingSessionId = secondSession.TrainingSessionId, AthleteId = athlete.AthleteId, AthleteCodeSnapshot = athlete.AthleteCode, AthleteNameSnapshot = athlete.FullName, Status = AttendanceStatus.Late });
        await db.SaveChangesAsync();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var summary = Assert.Single(result!.AttendanceSummary);
        Assert.Equal("นักกีฬา", summary.AthleteName);
        Assert.Equal(2, summary.AttendanceCount);
    }

    [Fact]
    public async Task GetCalendarAsync_RejectsRangeLongerThanSixtyThreeDays()
    {
        using var db = TestDbContextFactory.Create();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));

        Assert.Null(result);
    }
}
