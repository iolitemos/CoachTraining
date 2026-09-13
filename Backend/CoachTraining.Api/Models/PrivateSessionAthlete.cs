namespace CoachTraining.Api.Models;

/// <summary>
/// Links a Private Training session to one assigned athlete (requirement.md
/// FR-PRIVATE-003/004). Snapshot fields preserve the athlete's identity at
/// assignment time per NFR-002.
/// </summary>
public class PrivateSessionAthlete
{
    public int PrivateSessionAthleteId { get; set; }

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public int? AthleteId { get; set; }
    public Athlete? Athlete { get; set; }

    public bool IsGuest { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestRemark { get; set; }

    public string AthleteCodeSnapshot { get; set; } = string.Empty;
    public string AthleteNameSnapshot { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
}
