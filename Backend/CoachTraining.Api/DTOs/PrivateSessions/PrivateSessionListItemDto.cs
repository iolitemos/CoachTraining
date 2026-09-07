using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class PrivateSessionListItemDto
{
    public int TrainingSessionId { get; set; }
    public DateOnly SessionDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public SessionStatus Status { get; set; }
    public int AthleteCount { get; set; }
}
