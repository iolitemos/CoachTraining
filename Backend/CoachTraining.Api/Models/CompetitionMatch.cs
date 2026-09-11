using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>Competition match details maintained by administrators.</summary>
public class CompetitionMatch : AuditableEntity
{
    public int CompetitionMatchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
