using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>Business role (Administrator, Coach, Management/Viewer) — see Constants.Roles.</summary>
public class Role : AuditableEntity
{
    public int RoleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
