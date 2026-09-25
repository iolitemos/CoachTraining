using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>Secret athlete-specific access link for parent Routine participation planning.</summary>
public class ParentRoutinePlanLink : AuditableEntity
{
    public int ParentRoutinePlanLinkId { get; set; }
    public int AthleteId { get; set; }
    public Athlete Athlete { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public string TokenHint { get; set; } = string.Empty;
    public string? ProtectedToken { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
}
