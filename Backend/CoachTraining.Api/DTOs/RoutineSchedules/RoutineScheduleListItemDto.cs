namespace CoachTraining.Api.DTOs.RoutineSchedules;

public class RoutineScheduleListItemDto
{
    public int RoutineScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateOnly EffectiveStartDate { get; set; }
    public DateOnly? EffectiveEndDate { get; set; }
    public bool IsActive { get; set; }
}
