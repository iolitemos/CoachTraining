using CoachTraining.Api.DTOs.Reports;

namespace CoachTraining.Api.Services;

/// <summary>Athlete Attendance Report (requirement.md 6.19, FR-RPT-ATH-001–007, todo.md 4.19).</summary>
public interface IAthleteAttendanceReportService
{
    Task<AthleteAttendanceReportResponseDto> GetReportAsync(AthleteAttendanceReportFilter filter);
}
