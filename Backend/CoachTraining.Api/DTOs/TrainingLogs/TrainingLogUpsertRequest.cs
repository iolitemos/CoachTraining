namespace CoachTraining.Api.DTOs.TrainingLogs;

/// <summary>No field is individually required — requirement.md 4.9 leaves content to the Coach's discretion.</summary>
public class TrainingLogUpsertRequest
{
    public string? Topic { get; set; }
    public string? Objective { get; set; }
    public string? ExerciseDrill { get; set; }
    public string? Focus { get; set; }
    public string? Intensity { get; set; }
    public string? CoachNotes { get; set; }
    public string? AthleteNotes { get; set; }
    public string? GeneralRemarks { get; set; }
}

public class TrainingLogActionResult
{
    public TrainingLogDto? TrainingLog { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
}
