using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>System user / login account (requirement.md 4.1, FR-USER-*).</summary>
public class User : AuditableEntity
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Optional link to the Coach record this account belongs to (FR-COACH-004).</summary>
    public Coach? Coach { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
