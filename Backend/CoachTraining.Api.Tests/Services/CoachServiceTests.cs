using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class CoachServiceTests
{
    private static CoachService CreateService(Data.ApplicationDbContext db) =>
        new(db, NullLogger<CoachService>.Instance);

    [Fact]
    public async Task CreateAsync_WithDuplicateCoachCode_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        await service.CreateAsync(new CoachCreateDto { CoachCode = "C001", FullName = "Coach A" }, actionByUserId: 1);
        var (result, error) = await service.CreateAsync(new CoachCreateDto { CoachCode = "C001", FullName = "Coach B" }, actionByUserId: 1);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesActiveCoach()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var (result, error) = await service.CreateAsync(new CoachCreateDto { CoachCode = "C001", FullName = "Coach A" }, actionByUserId: 1);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.True(result!.IsActive);
    }

    [Fact]
    public async Task CreateAndUpdateAsync_PersistsAdministratorOnlyCompensationReferences()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var (created, createError) = await service.CreateAsync(
            new CoachCreateDto
            {
                CoachCode = "C001",
                FullName = "Coach A",
                BaseSalary = 25_000.50m,
                PrivateRate = 750m,
            },
            actionByUserId: 1);

        Assert.Null(createError);
        Assert.Equal(25_000.50m, created!.BaseSalary);
        Assert.Equal(750m, created.PrivateRate);

        var (updated, updateError) = await service.UpdateAsync(
            created.CoachId,
            new CoachUpdateDto
            {
                FullName = "Coach A",
                BaseSalary = 30_000m,
                PrivateRate = 900.25m,
            },
            actionByUserId: 1);

        Assert.Null(updateError);
        Assert.Equal(30_000m, updated!.BaseSalary);
        Assert.Equal(900.25m, updated.PrivateRate);
    }

    [Fact]
    public void CoachRequests_WithNegativeCompensationReferences_AreInvalid()
    {
        var createRequest = new CoachCreateDto
        {
            CoachCode = "C001",
            FullName = "Coach A",
            BaseSalary = -1m,
        };
        var updateRequest = new CoachUpdateDto
        {
            FullName = "Coach A",
            PrivateRate = -0.01m,
        };

        Assert.False(IsValid(createRequest));
        Assert.False(IsValid(updateRequest));
    }

    [Fact]
    public async Task SetStatusAsync_Deactivate_DoesNotDeleteTheCoachRecord()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (created, _) = await service.CreateAsync(new CoachCreateDto { CoachCode = "C001", FullName = "Coach A" }, actionByUserId: 1);

        var success = await service.SetStatusAsync(created!.CoachId, isActive: false, actionByUserId: 1);
        var detail = await service.GetByIdAsync(created.CoachId);

        Assert.True(success);
        Assert.NotNull(detail);
        Assert.False(detail!.IsActive);
    }

    [Fact]
    public async Task GetActiveOptionsAsync_ExcludesInactiveCoaches()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);
        var (active, _) = await service.CreateAsync(new CoachCreateDto { CoachCode = "C001", FullName = "Active Coach" }, actionByUserId: 1);
        var (inactive, _) = await service.CreateAsync(new CoachCreateDto { CoachCode = "C002", FullName = "Inactive Coach" }, actionByUserId: 1);
        await service.SetStatusAsync(inactive!.CoachId, isActive: false, actionByUserId: 1);

        var options = await service.GetActiveOptionsAsync();

        Assert.Contains(options, o => o.CoachId == active!.CoachId);
        Assert.DoesNotContain(options, o => o.CoachId == inactive.CoachId);
    }

    private static bool IsValid(object request)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
    }
}
