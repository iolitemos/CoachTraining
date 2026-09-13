using CoachTraining.Api.Models;
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
            new RoutineSchedule { CoachId = coach.CoachId, EffectiveStartDate = new DateOnly(2026, 9, 12), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsActive = true },
            new RoutineSchedule { CoachId = coach.CoachId, EffectiveStartDate = new DateOnly(2026, 9, 13), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), IsActive = false });
        await db.SaveChangesAsync();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        var item = Assert.Single(result!.Schedules);
        Assert.Equal("ปิง", item.CoachNickname);
        Assert.Equal("#123456", item.CoachColorHex);
        Assert.Equal(new DateOnly(2026, 9, 12), item.TrainingDate);
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
    public async Task GetCalendarAsync_RejectsRangeLongerThanSixtyThreeDays()
    {
        using var db = TestDbContextFactory.Create();
        var service = new PublicRoutineCalendarService(db, NullLogger<PublicRoutineCalendarService>.Instance);
        var link = await service.RotateLinkAsync(7);

        var result = await service.GetCalendarAsync(link.Token, new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));

        Assert.Null(result);
    }
}
