using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Reports;

/// <summary>Athlete Attendance Report filters (requirement.md 6.19, FR-RPT-ATH-003/004).</summary>
public class AthleteAttendanceReportFilter
{
    public int? AthleteId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

/// <summary>One recorded attendance entry, for the per-athlete history (FR-RPT-ATH-001).</summary>
public class AthleteAttendanceRecordDto
{
    public int TrainingSessionId { get; set; }
    public TrainingType TrainingType { get; set; }
    public DateOnly SessionDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public string? Remark { get; set; }
}

/// <summary>One athlete's attendance summary and history for the filtered range.</summary>
public class AthleteAttendanceReportItemDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Routine supports Present/Late only (FR-RATT-005) — recorded attendance
    /// only, never inferred (FR-RPT-ATH-006).</summary>
    public int RoutinePresentCount { get; set; }
    public int RoutineLateCount { get; set; }

    /// <summary>Private supports the full status set (FR-RPT-ATH-005).</summary>
    public int PrivatePresentCount { get; set; }
    public int PrivateAbsentCount { get; set; }
    public int PrivateLateCount { get; set; }
    public int PrivateExcusedCount { get; set; }

    public List<AthleteAttendanceRecordDto> Records { get; set; } = [];
}

/// <summary>Athlete Attendance Report response (requirement.md 6.19, todo.md 4.19).</summary>
public class AthleteAttendanceReportResponseDto
{
    public List<AthleteAttendanceReportItemDto> Items { get; set; } = [];
}
