namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class RoutineScheduleListItemDto
{
    public int RoutineScheduleId { get; set; }
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public string? CoachNickname { get; set; }
    public string CoachColorHex { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateOnly EffectiveStartDate { get; set; }
    public bool IsActive { get; set; }
}
