using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Reports;

/// <summary>Coach Teaching-Hour Report filters (requirement.md 6.18, FR-RPT-COACH-004–006).</summary>
public class CoachTeachingHourReportFilter
{
    public int? CoachId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TrainingType? TrainingType { get; set; }
}

/// <summary>One coach's teaching-hour totals for the filtered range.</summary>
public class CoachTeachingHourReportItemDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal RoutineHours { get; set; }
    public decimal PrivateHours { get; set; }
    public decimal TotalHours { get; set; }
}

/// <summary>Coach Teaching-Hour Report response (requirement.md 6.18, todo.md 4.18).</summary>
public class CoachTeachingHourReportResponseDto
{
    public List<CoachTeachingHourReportItemDto> Items { get; set; } = [];
    public decimal TotalRoutineHours { get; set; }
    public decimal TotalPrivateHours { get; set; }
    public decimal GrandTotalHours { get; set; }
}
