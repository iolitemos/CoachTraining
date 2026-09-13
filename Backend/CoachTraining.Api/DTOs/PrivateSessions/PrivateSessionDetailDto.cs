using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class PrivateSessionDetailDto
{
    public int TrainingSessionId { get; set; }
    public DateOnly SessionDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Remarks { get; set; }
    public SessionStatus Status { get; set; }

    /// <summary>FR-CONFLICT-004/005 — set when this session was saved despite a detected conflict.</summary>
    public bool IsConflictOverridden { get; set; }
    public string? ConflictOverrideReason { get; set; }

    public List<PrivateSessionAthleteDto> Athletes { get; set; } = [];
}

public class PrivateSessionAthleteDto
{
    public int PrivateSessionAthleteId { get; set; }
    public int? AthleteId { get; set; }
    public bool IsGuest { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? GuestPhone { get; set; }
    public string? GuestRemark { get; set; }
}
