namespace CoachTraining.Api.Helpers;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";
    public string FrontendResetUrl { get; set; } = string.Empty;
    public int TokenExpiryMinutes { get; set; } = 10;
}
