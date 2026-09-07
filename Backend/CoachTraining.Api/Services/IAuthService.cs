using CoachTraining.Api.DTOs.Auth;

namespace CoachTraining.Api.Services;

public interface IAuthService
{
    /// <summary>Returns null when the username/password combination is invalid or the account is inactive.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
