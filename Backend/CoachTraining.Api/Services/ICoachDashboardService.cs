using CoachTraining.Api.DTOs.Dashboards;

namespace CoachTraining.Api.Services;

/// <summary>Coach Home dashboard (requirement.md 6.16, FR-CDASH-001–003, todo.md 4.16).</summary>
public interface ICoachDashboardService
{
    /// <summary>Data is always scoped to <paramref name="coachId"/> — there is no
    /// cross-coach view of this dashboard.</summary>
    Task<CoachDashboardResponseDto> GetDashboardAsync(int coachId);
}
