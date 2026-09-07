namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class GenerateSessionsRequest
{
    /// <summary>Generate occurrences up to and including this date (capped at the schedule's EffectiveEndDate).</summary>
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
