namespace CoachTraining.Api.Models;

/// <summary>What was taught during a session (requirement.md 4.9, FR-LOG-*). One per TrainingSession.</summary>
public class TrainingLog
{
    public int TrainingLogId { get; set; }

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public string? Topic { get; set; }
    public string? Objective { get; set; }
    public string? ExerciseDrill { get; set; }
    public string? Focus { get; set; }
    public string? Intensity { get; set; }
    public string? CoachNotes { get; set; }
    public string? AthleteNotes { get; set; }
    public string? GeneralRemarks { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
}
