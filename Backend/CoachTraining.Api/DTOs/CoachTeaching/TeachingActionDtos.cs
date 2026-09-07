using CoachTraining.Api.DTOs.TrainingSessions;

namespace CoachTraining.Api.DTOs.CoachTeaching;

/// <summary>
/// Optional client-supplied timestamp (the coach's device clock, since the gym
/// operates on one local timezone with no server-side conversion — see
/// TrainingSessionConfiguration). Falls back to server time when omitted.
/// </summary>
public class TeachingStartRequest
{
    public DateTime? ActualStartDateTime { get; set; }
}

public class TeachingEndRequest
{
    public DateTime? ActualEndDateTime { get; set; }
}

public class TeachingActionResult
{
    public TrainingSessionDetailDto? Session { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
}
