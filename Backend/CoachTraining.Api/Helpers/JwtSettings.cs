namespace CoachTraining.Api.Helpers;

/// <summary>Bound from the "Jwt" configuration section. SigningKey must come from
/// user-secrets (Development) or an environment variable (QAS/PROD) — never committed.</summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}
