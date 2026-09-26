using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoachTraining.Api.Tests.Services;

public class ParentRoutinePlanServiceTests
{
    [Fact]
    public async Task SaveAsync_AddsAndClearsOnlyExplicitPlansWithoutCreatingAttendance()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        var service = CreateService(db);
        var link = await service.RotateLinkAsync(athlete.AthleteId, 7);

        var selected = await service.SaveAsync(link!.Token, trainingDate, trainingDate, [trainingDate]);

        Assert.True(Assert.Single(selected!.Dates).IsSelected);
        Assert.Single(db.RoutineParticipationPlans);
        Assert.Empty(db.Attendances);

        var cleared = await service.SaveAsync(link.Token, trainingDate, trainingDate, []);

        Assert.False(Assert.Single(cleared!.Dates).IsSelected);
        Assert.Empty(await db.RoutineParticipationPlans.ToListAsync());
        Assert.Empty(db.Attendances);
    }

    [Fact]
    public async Task SaveAsync_RejectsDateWithoutActiveRoutineTraining()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        var service = CreateService(db);
        var link = await service.RotateLinkAsync(athlete.AthleteId, 7);
        var unavailableDate = trainingDate.AddDays(1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAsync(link!.Token, trainingDate, unavailableDate, [unavailableDate]));
    }

    [Fact]
    public async Task LinkRotationAndAccessState_InvalidatePublicAccess()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        var service = CreateService(db);
        var first = await service.RotateLinkAsync(athlete.AthleteId, 7);
        var second = await service.RotateLinkAsync(athlete.AthleteId, 7);
        var recovered = await service.GetLinkStatusAsync(athlete.AthleteId);

        Assert.Null(await service.GetCalendarAsync(first!.Token, trainingDate, trainingDate));
        Assert.NotNull(await service.GetCalendarAsync(second!.Token, trainingDate, trainingDate));
        Assert.Equal(second.Token, recovered!.Token);

        await service.SetLinkAccessAsync(athlete.AthleteId, false, 7);
        Assert.Null(await service.GetCalendarAsync(second.Token, trainingDate, trainingDate));

        await service.SetLinkAccessAsync(athlete.AthleteId, true, 7);
        Assert.NotNull(await service.GetCalendarAsync(second.Token, trainingDate, trainingDate));
    }

    [Fact]
    public async Task InactiveAthlete_CannotUseExistingLink()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        var service = CreateService(db);
        var link = await service.RotateLinkAsync(athlete.AthleteId, 7);
        athlete.IsActive = false;
        await db.SaveChangesAsync();

        Assert.Null(await service.GetCalendarAsync(link!.Token, trainingDate, trainingDate));
    }

    [Fact]
    public async Task AdministratorSummary_ReturnsCountAndAthleteNamesForRoutineDate()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        var service = CreateService(db);
        var link = await service.RotateLinkAsync(athlete.AthleteId, 7);
        await service.SaveAsync(link!.Token, trainingDate, trainingDate, [trainingDate]);

        var summary = Assert.Single(await service.GetSummaryAsync(trainingDate, trainingDate));
        var plannedAthlete = Assert.Single((await service.GetAthletesAsync(trainingDate))!);

        Assert.Equal(1, summary.AthleteCount);
        Assert.Equal(athlete.AthleteId, plannedAthlete.AthleteId);
        Assert.Equal(athlete.FullName, plannedAthlete.AthleteName);
    }

    [Fact]
    public async Task GetCalendarAsync_ReturnsCompetitionMatchesOverlappingRequestedRange()
    {
        using var db = TestDbContextFactory.Create();
        var (athlete, trainingDate) = await SeedAsync(db);
        db.CompetitionMatches.Add(new CompetitionMatch
        {
            Name = "Youth Championship",
            Province = "Bangkok",
            StartDate = trainingDate.AddDays(-1),
            EndDate = trainingDate.AddDays(1),
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var link = await service.RotateLinkAsync(athlete.AthleteId, 7);

        var result = await service.GetCalendarAsync(link!.Token, trainingDate, trainingDate);

        var competition = Assert.Single(result!.CompetitionMatches);
        Assert.Equal("Youth Championship", competition.Name);
        Assert.Equal(trainingDate.AddDays(-1), competition.StartDate);
        Assert.Equal(trainingDate.AddDays(1), competition.EndDate);
    }

    private static async Task<(Athlete Athlete, DateOnly TrainingDate)> SeedAsync(CoachTraining.Api.Data.ApplicationDbContext db)
    {
        var athlete = new Athlete { AthleteCode = "A001", FullName = "Athlete One", Nickname = "หนึ่ง", IsActive = true };
        db.Add(athlete);
        await db.SaveChangesAsync();
        var trainingDate = new DateOnly(2026, 10, 3);
        db.RoutineTrainingDates.Add(new RoutineTrainingDate { TrainingDate = trainingDate });
        await db.SaveChangesAsync();
        return (athlete, trainingDate);
    }

    private static ParentRoutinePlanService CreateService(CoachTraining.Api.Data.ApplicationDbContext db) =>
        new(db, new EphemeralDataProtectionProvider(), NullLogger<ParentRoutinePlanService>.Instance);
}
