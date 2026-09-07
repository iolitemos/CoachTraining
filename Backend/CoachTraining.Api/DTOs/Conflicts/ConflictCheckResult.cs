namespace CoachTraining.Api.DTOs.Conflicts;

/// <summary>One concrete schedule conflict found by <see cref="Services.IScheduleConflictService"/>.</summary>
public class ConflictDetail
{
    public string ConflictType { get; set; } = string.Empty;
    public int ConflictingTrainingSessionId { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Populated only for AthleteOverlap conflicts.</summary>
    public int? AthleteId { get; set; }
    public string? AthleteName { get; set; }
}
