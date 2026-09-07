namespace CoachTraining.Api.Models;

/// <summary>Substitute coach audit trail (requirement.md 4.10, FR-SUB-*).</summary>
public class CoachSubstitutionHistory
{
    public int CoachSubstitutionHistoryId { get; set; }

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public int OriginalCoachId { get; set; }
    public Coach OriginalCoach { get; set; } = null!;

    public int SubstituteCoachId { get; set; }
    public Coach SubstituteCoach { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;

    public int ActionByUserId { get; set; }
    public User ActionByUser { get; set; } = null!;

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
