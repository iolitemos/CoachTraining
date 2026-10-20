using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>One finalized Private Training session credited to a monthly statement.</summary>
public class CoachCompensationItem : AuditableEntity
{
    public int CoachCompensationItemId { get; set; }

    public int CoachCompensationStatementId { get; set; }
    public CoachCompensationStatement Statement { get; set; } = null!;

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public DateOnly SessionDateSnapshot { get; set; }
    public DateTime ScheduledStartSnapshot { get; set; }
    public DateTime ScheduledEndSnapshot { get; set; }
    public string CoachNameSnapshot { get; set; } = string.Empty;
    public string ParticipantNamesSnapshot { get; set; } = string.Empty;
    public decimal PrivateRateSnapshot { get; set; }
    public decimal Amount { get; set; }
}
