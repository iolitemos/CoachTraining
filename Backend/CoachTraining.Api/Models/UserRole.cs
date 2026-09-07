namespace CoachTraining.Api.Models;

/// <summary>User-to-role assignment (many-to-many join).</summary>
public class UserRole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
}
