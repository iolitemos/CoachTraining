namespace CoachTraining.Api.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public CurrentUserDto User { get; set; } = null!;
}
