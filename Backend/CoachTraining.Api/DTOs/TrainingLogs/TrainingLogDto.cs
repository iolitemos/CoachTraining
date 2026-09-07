namespace CoachTraining.Api.DTOs.TrainingLogs;

/// <summary>Null <see cref="TrainingLogId"/> means no log has been saved for the session yet.</summary>
public class TrainingLogDto
{
    public int? TrainingLogId { get; set; }
    public string? Topic { get; set; }
    public string? Objective { get; set; }
    public string? ExerciseDrill { get; set; }
    public string? Focus { get; set; }
    public string? Intensity { get; set; }
    public string? CoachNotes { get; set; }
    public string? AthleteNotes { get; set; }
    public string? GeneralRemarks { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
