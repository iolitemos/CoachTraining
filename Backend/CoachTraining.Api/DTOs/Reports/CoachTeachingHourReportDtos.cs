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

/// <summary>One coach's distinct teaching-day totals for the filtered range.</summary>
public class CoachTeachingHourReportItemDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public string? CoachNickname { get; set; }
    public string CoachColorHex { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int PlannedSessionCount { get; set; }
    public int ActualSessionCount { get; set; }
    public int PlannedDays { get; set; }
    public int ActualDays { get; set; }
    public int RoutineDays { get; set; }
    public int PrivateDays { get; set; }
    public int TotalDays { get; set; }
}

/// <summary>Coach Teaching-Hour Report response (requirement.md 6.18, todo.md 4.18).</summary>
public class CoachTeachingHourReportResponseDto
{
    public List<CoachTeachingHourReportItemDto> Items { get; set; } = [];
    public int TotalRoutineDays { get; set; }
    public int TotalPrivateDays { get; set; }
    public int GrandTotalDays { get; set; }
    public int TotalPlannedDays { get; set; }
    public int TotalActualDays { get; set; }
}
