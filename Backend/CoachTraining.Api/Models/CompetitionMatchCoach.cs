namespace CoachTraining.Api.Models;

/// <summary>Links a Competition Match to each coach responsible for its athletes.</summary>
public class CompetitionMatchCoach
{
    public int CompetitionMatchCoachId { get; set; }
    public int CompetitionMatchId { get; set; }
    public CompetitionMatch CompetitionMatch { get; set; } = null!;
    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;
    public string CoachNameSnapshot { get; set; } = string.Empty;
    public string? CoachNicknameSnapshot { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
}
