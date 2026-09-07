using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>Athlete master data (requirement.md 4.3, FR-ATHLETE-*).</summary>
public class Athlete : AuditableEntity
{
    public int AthleteId { get; set; }

    public string AthleteCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Nickname { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ParentName { get; set; }

    public string? ParentPhoneNumber { get; set; }

    public string? AthleteLevel { get; set; }

    public DateOnly? JoinDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remarks { get; set; }
}
