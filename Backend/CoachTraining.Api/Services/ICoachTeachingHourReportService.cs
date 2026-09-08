using CoachTraining.Api.DTOs.Reports;

namespace CoachTraining.Api.Services;

/// <summary>Coach Teaching-Hour Report (requirement.md 6.18, FR-RPT-COACH-001–009, todo.md 4.18).</summary>
public interface ICoachTeachingHourReportService
{
    Task<CoachTeachingHourReportResponseDto> GetReportAsync(CoachTeachingHourReportFilter filter);
}
