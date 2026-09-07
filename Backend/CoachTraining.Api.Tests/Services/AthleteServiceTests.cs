using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AthleteServiceTests
{
    private static AthleteService CreateService(Data.ApplicationDbContext db) =>
        new(db, NullLogger<AthleteService>.Instance);

    [Fact]
    public async Task CreateAsync_WithDuplicateAthleteCode_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete A" }, actionByUserId: 1);
        var (result, error) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete B" }, actionByUserId: 1);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task SetStatusAsync_Deactivate_DoesNotDeleteTheAthleteRecord()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (created, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Athlete A" }, actionByUserId: 1);

        var success = await service.SetStatusAsync(created!.AthleteId, isActive: false, actionByUserId: 1);
        var detail = await service.GetByIdAsync(created.AthleteId);

        Assert.True(success);
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);
    }

    [Fact]
    public async Task SearchActiveAsync_ExcludesInactiveAthletes()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (active, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Active Athlete" }, actionByUserId: 1);
        var (inactive, _) = await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", FullName = "Inactive Athlete" }, actionByUserId: 1);
        await service.SetStatusAsync(inactive!.AthleteId, isActive: false, actionByUserId: 1);

        var results = await service.SearchActiveAsync(null);

        Assert.Contains(results, a => a.AthleteId == active!.AthleteId);
        Assert.DoesNotContain(results, a => a.AthleteId == inactive.AthleteId);
    }

    [Fact]
    public async Task SearchActiveAsync_FiltersByNameOrCode()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A001", FullName = "Somchai Jaidee" }, actionByUserId: 1);
        await service.CreateAsync(new AthleteCreateDto { AthleteCode = "A002", FullName = "Somsri Rakdee" }, actionByUserId: 1);

        var results = await service.SearchActiveAsync("Somchai");

        Assert.Single(results);
        Assert.Equal("A001", results[0].AthleteCode);
    }
}
