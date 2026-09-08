using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Dashboards;

/// <summary>Administrator Dashboard filters (requirement.md 6.17, FR-ADASH-001–003).
/// An unset date range defaults to today, matching the "Training Sessions Today"
/// anchor metric.</summary>
public class AdministratorDashboardFilterRequest
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? CoachId { get; set; }
    public TrainingType? TrainingType { get; set; }
}

/// <summary>One coach's session load for today, with identifiers for FR-ADASH-004
/// ("open the related operational records").</summary>
public class CoachTeachingTodayDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public List<int> TrainingSessionIds { get; set; } = [];
}

/// <summary>Aggregate athlete attendance counts across the filtered range.</summary>
public class AttendanceSummaryDto
{
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
}

/// <summary>One coach's teaching-hour totals across the filtered range.</summary>
public class CoachTeachingHoursDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string CoachFullName { get; set; } = string.Empty;
    public decimal RoutineHours { get; set; }
    public decimal PrivateHours { get; set; }
    public decimal TotalHours { get; set; }
}

/// <summary>Administrator Dashboard response (requirement.md 6.17, todo.md 4.17).</summary>
public class AdministratorDashboardResponseDto
{
    public int SessionsTodayCount { get; set; }
    public int CompletedCount { get; set; }
    public int UpcomingCount { get; set; }
    public int CancelledCount { get; set; }
    public int RoutineCount { get; set; }
    public int PrivateCount { get; set; }
    public List<CoachTeachingTodayDto> CoachesTeachingToday { get; set; } = [];
    public AttendanceSummaryDto AttendanceSummary { get; set; } = new();
    public List<CoachTeachingHoursDto> CoachTeachingHours { get; set; } = [];
}
