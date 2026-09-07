using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>Coach master data (requirement.md 4.2, FR-COACH-*).</summary>
public class Coach : AuditableEntity
{
    public int CoachId { get; set; }

    public string CoachCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? CoachType { get; set; }

    public string? Specialization { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remarks { get; set; }

    /// <summary>Optional link to the coach's own login account (FR-COACH-004).</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
}
