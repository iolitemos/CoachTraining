using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>A revocable secret link granting read-only access to the public Routine calendar.</summary>
public class RoutineCalendarShareLink : AuditableEntity
{
    public int RoutineCalendarShareLinkId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string TokenHint { get; set; } = string.Empty;
    public DateTime? RevokedAtUtc { get; set; }
    public int? RevokedByUserId { get; set; }
}
