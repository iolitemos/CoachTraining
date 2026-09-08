using CoachTraining.Api.DTOs.PrivateSessions;
using CoachTraining.Api.DTOs.TrainingLogs;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.TrainingSessions;

public class TrainingSessionDetailDto
{
    public int TrainingSessionId { get; set; }
    public TrainingType TrainingType { get; set; }
    public int? RoutineScheduleId { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }
    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }

    /// <summary>FR-TEACH-003 — computed from Actual Start/End, not stored.</summary>
    public int? ActualDurationMinutes { get; set; }

    public int AssignedCoachId { get; set; }
    public string AssignedCoachCode { get; set; } = string.Empty;
    public string AssignedCoachName { get; set; } = string.Empty;
    public string? AssignedCoachNickname { get; set; }
    public string AssignedCoachColorHex { get; set; } = "#10B981";

    public int? ActualCoachId { get; set; }
    public string? ActualCoachCode { get; set; }
    public string? ActualCoachName { get; set; }
    public string? ActualCoachNickname { get; set; }
    public string? ActualCoachColorHex { get; set; }

    public SessionStatus Status { get; set; }
    public string? Location { get; set; }
    public string? Remarks { get; set; }
    public string? CancellationReason { get; set; }

    public int? OriginalSessionId { get; set; }
    public bool IsConflictOverridden { get; set; }
    public string? ConflictOverrideReason { get; set; }

    /// <summary>Populated only for Private Training (Routine has no fixed roster).</summary>
    public List<PrivateSessionAthleteDto> Athletes { get; set; } = [];

    /// <summary>Null TrainingLogId means no log has been saved for this session yet (FR-LOG-003).</summary>
    public TrainingLogDto? TrainingLog { get; set; }
}
