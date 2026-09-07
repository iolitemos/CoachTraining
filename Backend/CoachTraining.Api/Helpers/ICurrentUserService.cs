namespace CoachTraining.Api.Helpers;

/// <summary>
/// Exposes the signed-in user's identity and roles to Services without
/// requiring direct access to HttpContext. Populated once JWT authentication
/// (todo.md section 3) issues the corresponding claims.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The signed-in user's UserId claim, or null when not authenticated.</summary>
    int? UserId { get; }

    /// <summary>The signed-in Coach's CoachId, when this account is linked to a Coach record (FR-COACH-004).</summary>
    int? CoachId { get; }

    /// <summary>The signed-in user's roles.</summary>
    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
