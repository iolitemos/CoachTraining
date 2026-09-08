using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class PasswordServiceTests
{
    private static readonly IPasswordHasher<User> Hasher = new PasswordHasher<User>();

    [Fact]
    public async Task ChangePasswordAsync_RequiresCorrectCurrentPassword()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var service = CreateService(db, new FakeEmailSender());

        var error = await service.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
        {
            CurrentPassword = "wrong-password",
            NewPassword = "NewPassword123!",
        });

        Assert.Equal("รหัสผ่านปัจจุบันไม่ถูกต้อง", error);
        Assert.NotEqual(PasswordVerificationResult.Success, Hasher.VerifyHashedPassword(user, user.PasswordHash, "NewPassword123!"));
    }

    [Fact]
    public async Task ChangePasswordAsync_ChangesOnlyRequestedUserPassword()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var other = await SeedUserAsync(db, "other@test.local");
        var otherHash = other.PasswordHash;
        var service = CreateService(db, new FakeEmailSender());

        var error = await service.ChangePasswordAsync(user.UserId, new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
        });

        Assert.Null(error);
        Assert.Equal(PasswordVerificationResult.Success, Hasher.VerifyHashedPassword(user, user.PasswordHash, "NewPassword123!"));
        Assert.Equal(otherHash, other.PasswordHash);
    }

    [Fact]
    public async Task RequestResetAsync_SendsRegisteredEmailAndCreatesTenMinuteToken()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sender = new FakeEmailSender();
        var before = DateTime.UtcNow;

        await CreateService(db, sender).RequestResetAsync(new ForgotPasswordRequest { Email = " OWNER@TEST.LOCAL " });

        var token = Assert.Single(db.PasswordResetTokens);
        Assert.Equal(user.Email, sender.RecipientEmail);
        Assert.InRange(token.ExpiresAtUtc, before.AddMinutes(10), DateTime.UtcNow.AddMinutes(10));
        Assert.DoesNotContain(ReadToken(sender.ResetUrl!), token.TokenHash);
    }

    [Fact]
    public async Task RequestResetAsync_InvalidatesPreviousUnusedToken()
    {
        using var db = TestDbContextFactory.Create();
        await SeedUserAsync(db);
        var sender = new FakeEmailSender();
        var service = CreateService(db, sender);
        await service.RequestResetAsync(new ForgotPasswordRequest { Email = "owner@test.local" });
        var first = db.PasswordResetTokens.Single();

        await service.RequestResetAsync(new ForgotPasswordRequest { Email = "owner@test.local" });

        Assert.NotNull(first.InvalidatedAtUtc);
        Assert.Equal(2, db.PasswordResetTokens.Count());
    }

    [Fact]
    public async Task ResetPasswordAsync_IsSingleUse()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sender = new FakeEmailSender();
        var service = CreateService(db, sender);
        await service.RequestResetAsync(new ForgotPasswordRequest { Email = user.Email });
        var rawToken = ReadToken(sender.ResetUrl!);
        var request = new ResetPasswordRequest { Token = rawToken, NewPassword = "ResetPassword123!" };

        Assert.Null(await service.ResetPasswordAsync(request));
        Assert.NotNull(await service.ResetPasswordAsync(request));
        Assert.Equal(PasswordVerificationResult.Success, Hasher.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword));
    }

    [Fact]
    public async Task ResetPasswordAsync_RejectsExpiredToken()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sender = new FakeEmailSender();
        var service = CreateService(db, sender);
        await service.RequestResetAsync(new ForgotPasswordRequest { Email = user.Email });
        db.PasswordResetTokens.Single().ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        var rawToken = ReadToken(sender.ResetUrl!);

        var error = await service.ResetPasswordAsync(new ResetPasswordRequest { Token = rawToken, NewPassword = "ResetPassword123!" });

        Assert.NotNull(error);
    }

    [Fact]
    public async Task RequestResetAsync_UnknownEmailReturnsWithoutSending()
    {
        using var db = TestDbContextFactory.Create();
        var sender = new FakeEmailSender();

        await CreateService(db, sender).RequestResetAsync(new ForgotPasswordRequest { Email = "unknown@test.local" });

        Assert.Null(sender.RecipientEmail);
        Assert.Empty(db.PasswordResetTokens);
    }

    [Fact]
    public async Task RequestResetAsync_InactiveAccountReturnsWithoutSending()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        user.IsActive = false;
        await db.SaveChangesAsync();
        var sender = new FakeEmailSender();

        await CreateService(db, sender).RequestResetAsync(new ForgotPasswordRequest { Email = user.Email });

        Assert.Null(sender.RecipientEmail);
        Assert.Empty(db.PasswordResetTokens);
    }

    [Theory]
    [InlineData(typeof(ChangePasswordRequest))]
    [InlineData(typeof(ResetPasswordRequest))]
    public void PasswordDtos_RejectPasswordsShorterThanEightCharacters(Type requestType)
    {
        object request = requestType == typeof(ChangePasswordRequest)
            ? new ChangePasswordRequest { CurrentPassword = "current", NewPassword = "1234567" }
            : new ResetPasswordRequest { Token = "token", NewPassword = "1234567" };

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains("NewPassword"));
    }

    private static PasswordService CreateService(Data.ApplicationDbContext db, IEmailSender sender) =>
        new(db, Hasher, sender, Options.Create(new PasswordResetSettings
        {
            FrontendResetUrl = "https://example.test/reset-password",
            TokenExpiryMinutes = 10,
        }), NullLogger<PasswordService>.Instance);

    private static string ReadToken(string resetUrl)
    {
        var fragment = new Uri(resetUrl).Fragment.TrimStart('#');
        return Uri.UnescapeDataString(fragment.Split('=', 2)[1]);
    }

    private static async Task<User> SeedUserAsync(Data.ApplicationDbContext db, string email = "owner@test.local")
    {
        var user = new User { Username = Guid.NewGuid().ToString("N"), Email = email, FullName = "Account Owner", IsActive = true };
        user.PasswordHash = Hasher.HashPassword(user, "OldPassword123!");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public string? RecipientEmail { get; private set; }
        public string? ResetUrl { get; private set; }
        public Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken = default)
        {
            RecipientEmail = recipientEmail;
            ResetUrl = resetUrl;
            return Task.CompletedTask;
        }
    }
}
