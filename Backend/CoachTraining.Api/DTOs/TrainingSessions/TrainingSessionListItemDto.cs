using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.TrainingSessions;

public class TrainingSessionListItemDto
{
    public int TrainingSessionId { get; set; }
    public int? RoutineScheduleId { get; set; }
    public TrainingType TrainingType { get; set; }
    public DateOnly SessionDate { get; set; }
    public DateTime ScheduledStartDateTime { get; set; }
    public DateTime ScheduledEndDateTime { get; set; }
    public DateTime? ActualStartDateTime { get; set; }
    public DateTime? ActualEndDateTime { get; set; }
    public string AssignedCoachCode { get; set; } = string.Empty;
    public string AssignedCoachName { get; set; } = string.Empty;
    public string? AssignedCoachNickname { get; set; }
    public string AssignedCoachColorHex { get; set; } = "#10B981";
    public string? ActualCoachCode { get; set; }
    public string? ActualCoachName { get; set; }
    public string? ActualCoachNickname { get; set; }
    public string? ActualCoachColorHex { get; set; }
    public SessionStatus Status { get; set; }
    public string? Location { get; set; }
}
