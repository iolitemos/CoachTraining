namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class GenerateSessionsRequest
{
    /// <summary>The selected schedule date must be on or before this date.</summary>
    public DateOnly ThroughDate { get; set; }
}

public class GenerateSessionsResult
{
    public int GeneratedCount { get; set; }
    public List<SkippedOccurrence> SkippedDueToConflict { get; set; } = [];
}

public class SkippedOccurrence
{
    public DateOnly SessionDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
