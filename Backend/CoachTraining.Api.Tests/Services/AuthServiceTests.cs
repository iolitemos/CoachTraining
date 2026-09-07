using CoachTraining.Api.Constants;
using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class AuthServiceTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    private static AuthService CreateService(Data.ApplicationDbContext db)
    {
        var jwtTokenService = new JwtTokenService(Options.Create(new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = "unit-test-signing-key-at-least-32-bytes-long",
            ExpiryMinutes = 60,
        }));

        return new AuthService(db, jwtTokenService, Hasher, NullLogger<AuthService>.Instance);
    }

    private static async Task<User> SeedUserAsync(Data.ApplicationDbContext db, string username, string password, bool isActive = true, string roleName = Roles.Coach)
    {
        var role = new Role { Name = roleName };
        db.Roles.Add(role);

        var user = new User
        {
            Username = username,
            Email = $"{username}@test.local",
            FullName = "Test User",
            IsActive = isActive,
        };
        user.PasswordHash = Hasher.HashPassword(user, password);
        user.UserRoles = [new UserRole { Role = role }];

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenAndUserInfo()
    {
        using var db = TestDbContextFactory.Create();
        await SeedUserAsync(db, "coach1", "CorrectPass123!");
        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest { Username = "coach1", Password = "CorrectPass123!" });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.Equal("coach1", result.User.Username);
        Assert.Contains(Roles.Coach, result.User.Roles);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        await SeedUserAsync(db, "coach1", "CorrectPass123!");
        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest { Username = "coach1", Password = "WrongPass!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveAccount_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        await SeedUserAsync(db, "coach1", "CorrectPass123!", isActive: false);
        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest { Username = "coach1", Password = "CorrectPass123!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUsername_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var service = CreateService(db);

        var result = await service.LoginAsync(new LoginRequest { Username = "nobody", Password = "whatever" });

        Assert.Null(result);
    }
}
