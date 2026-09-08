using System.Security.Cryptography;
using System.Text;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoachTraining.Api.Services;

public class PasswordService : IPasswordService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly PasswordResetSettings _settings;
    private readonly ILogger<PasswordService> _logger;

    public PasswordService(ApplicationDbContext db, IPasswordHasher<User> passwordHasher, IEmailSender emailSender,
        IOptions<PasswordResetSettings> settings, ILogger<PasswordService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string?> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(item => item.UserId == userId && item.IsActive, cancellationToken);
        if (user is null)
        {
            return "ไม่พบบัญชีผู้ใช้ที่ใช้งานอยู่";
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return "รหัสผ่านปัจจุบันไม่ถูกต้อง";
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedByUserId = userId;
        user.UpdatedDate = DateTime.UtcNow;
        await InvalidateActiveTokensAsync(userId, DateTime.UtcNow, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task RequestResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.IsActive && item.Email.ToUpper() == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await InvalidateActiveTokensAsync(user.UserId, now, cancellationToken);
        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = now.AddMinutes(_settings.TokenExpiryMinutes),
            CreatedByUserId = user.UserId,
        });
        await _db.SaveChangesAsync(cancellationToken);

        // Keep the secret in the URL fragment so it is not sent to the frontend web server or access logs.
        var resetUrl = $"{_settings.FrontendResetUrl}#token={Uri.EscapeDataString(rawToken)}";
        try
        {
            await _emailSender.SendPasswordResetAsync(user.Email, user.FullName, resetUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to send password-reset email. UserId: {UserId}", user.UserId);
        }
    }

    public async Task<string?> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.Token);
        var token = await _db.PasswordResetTokens.Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        var now = DateTime.UtcNow;
        if (token is null || token.UsedAtUtc is not null || token.InvalidatedAtUtc is not null || token.ExpiresAtUtc <= now || !token.User.IsActive)
        {
            return "ลิงก์ตั้งรหัสผ่านไม่ถูกต้อง หมดอายุ หรือถูกใช้งานแล้ว";
        }

        token.User.PasswordHash = _passwordHasher.HashPassword(token.User, request.NewPassword);
        token.User.UpdatedByUserId = token.UserId;
        token.User.UpdatedDate = now;
        token.UsedAtUtc = now;
        token.UpdatedByUserId = token.UserId;
        token.UpdatedDate = now;
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    private async Task InvalidateActiveTokensAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var activeTokens = await _db.PasswordResetTokens
            .Where(item => item.UserId == userId && item.UsedAtUtc == null && item.InvalidatedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.InvalidatedAtUtc = now;
            token.UpdatedByUserId = userId;
            token.UpdatedDate = now;
        }
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
