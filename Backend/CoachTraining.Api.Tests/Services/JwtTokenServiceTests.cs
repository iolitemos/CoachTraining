using System.IdentityModel.Tokens.Jwt;
using CoachTraining.Api.Constants;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoachTraining.Api.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryMinutes = 60) =>
        new(Options.Create(new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = "unit-test-signing-key-at-least-32-bytes-long",
            ExpiryMinutes = expiryMinutes,
        }));

    [Fact]
    public void GenerateToken_IncludesUserIdentityAndRoleClaims()
    {
        var service = CreateService();
        var user = new User { UserId = 42, Username = "coach1", Email = "coach1@test.local" };

        var (token, expiresAtUtc) = service.GenerateToken(user, [Roles.Coach]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("42", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        Assert.Equal("coach1", jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.Name).Value);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == Roles.Coach);
        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.True(expiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_IncludesAllAssignedRoles()
    {
        var service = CreateService();
        var user = new User { UserId = 1, Username = "admin", Email = "admin@test.local" };

        var (token, _) = service.GenerateToken(user, [Roles.Administrator, Roles.Coach]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var roleClaims = jwt.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains(Roles.Administrator, roleClaims);
        Assert.Contains(Roles.Coach, roleClaims);
    }
}
