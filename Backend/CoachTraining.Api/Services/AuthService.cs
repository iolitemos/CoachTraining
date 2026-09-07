using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Auth;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.Coach)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user is null || !user.IsActive)
            {
                return null;
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var (token, expiresAtUtc) = _jwtTokenService.GenerateToken(user, roles);

            return new LoginResponse
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new CurrentUserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = roles,
                    CoachId = user.Coach?.CoachId,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed unexpectedly. Controller: AuthController Service: AuthService Function: LoginAsync Username: {Username}", request.Username);
            throw;
        }
    }
}
