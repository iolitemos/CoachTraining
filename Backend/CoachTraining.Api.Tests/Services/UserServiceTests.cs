using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Users;
using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class UserServiceTests
{
    private static UserService CreateService(Data.ApplicationDbContext db) =>
        new(db, new PasswordHasher<User>(), new CoachService(db, NullLogger<CoachService>.Instance), NullLogger<UserService>.Instance);

    private static async Task<Role> SeedRoleAsync(Data.ApplicationDbContext db, string name = Roles.Coach)
    {
        var role = new Role { Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        await service.CreateAsync(
            new UserCreateDto { Username = "coach1", Email = "a@test.local", FullName = "A", Password = "Password123!", RoleIds = [role.RoleId] },
            actionByUserId: 1);

        var (result, error) = await service.CreateAsync(
            new UserCreateDto { Username = "coach1", Email = "b@test.local", FullName = "B", Password = "Password123!", RoleIds = [role.RoleId] },
            actionByUserId: 1);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsError()
    {
        using var db = TestDbContextFactory.Create();
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        await service.CreateAsync(
            new UserCreateDto { Username = "coach1", Email = "shared@test.local", FullName = "A", Password = "Password123!", RoleIds = [role.RoleId] },
            actionByUserId: 1);

        var (result, error) = await service.CreateAsync(
            new UserCreateDto { Username = "coach2", Email = "shared@test.local", FullName = "B", Password = "Password123!", RoleIds = [role.RoleId] },
            actionByUserId: 1);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_HashesPasswordAndAssignsRoles()
    {
        using var db = TestDbContextFactory.Create();
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        var (result, error) = await service.CreateAsync(
            new UserCreateDto { Username = "coach1", Email = "coach1@test.local", FullName = "Coach One", Password = "Password123!", RoleIds = [role.RoleId] },
            actionByUserId: 1);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("coach1", result!.Username);
        Assert.Contains(Roles.Coach, result.Roles);

        var stored = await db.Users.FindAsync(result.UserId);
        Assert.NotNull(stored);
        Assert.NotEqual("Password123!", stored!.PasswordHash);
    }
}
