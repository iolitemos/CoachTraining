using CoachTraining.Api.Models;

namespace CoachTraining.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user, IReadOnlyList<string> roles);
}
