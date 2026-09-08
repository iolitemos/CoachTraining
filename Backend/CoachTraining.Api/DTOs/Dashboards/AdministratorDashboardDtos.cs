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
    public string CoachNickname { get; set; } = string.Empty;
    public string CoachColorHex { get; set; } = string.Empty;
    public TrainingType TrainingType { get; set; }
    public int SessionCount { get; set; }
    public List<int> TrainingSessionIds { get; set; } = [];
}

/// <summary>One athlete who explicitly attended a session in the filtered range.</summary>
public class AthleteAttendanceSummaryItemDto
{
    public int AthleteId { get; set; }
    public string AthleteName { get; set; } = string.Empty;
    public int AttendanceCount { get; set; }
}

/// <summary>Participation-only attendance summary, separated by training type.</summary>
public class AttendanceByTrainingTypeDto
{
    public int TotalAttendance { get; set; }
    public List<AthleteAttendanceSummaryItemDto> Athletes { get; set; } = [];
    public List<DailyAttendanceSummaryDto> DailySummaries { get; set; } = [];
}

public class DailyAttendanceCellDto
{
    public int AthleteId { get; set; }
    public int AttendanceCount { get; set; }
}

public class DailyAttendanceCoachDto
{
    public int CoachId { get; set; }
    public string CoachNickname { get; set; } = string.Empty;
    public string CoachColorHex { get; set; } = string.Empty;
}

public class DailyAttendanceSummaryDto
{
    public DateOnly Date { get; set; }
    public int TotalAttendance { get; set; }
    public List<DailyAttendanceCellDto> Attendances { get; set; } = [];
    public List<DailyAttendanceCoachDto> Coaches { get; set; } = [];
}

public class AttendanceSummaryDto
{
    public AttendanceByTrainingTypeDto Routine { get; set; } = new();
    public AttendanceByTrainingTypeDto Private { get; set; } = new();
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
}
