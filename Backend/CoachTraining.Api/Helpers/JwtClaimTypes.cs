namespace CoachTraining.Api.Helpers;

/// <summary>Custom JWT claim type names, beyond the standard <see cref="System.Security.Claims.ClaimTypes"/>.</summary>
public static class JwtClaimTypes
{
    /// <summary>Set only when the signed-in User is linked to a Coach record (FR-COACH-004).</summary>
    public const string CoachId = "coachId";
}
