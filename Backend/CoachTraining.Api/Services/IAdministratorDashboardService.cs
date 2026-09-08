using CoachTraining.Api.DTOs.Dashboards;

namespace CoachTraining.Api.Services;

/// <summary>Administrator Dashboard (requirement.md 6.17, FR-ADASH-001–004, todo.md 4.17).</summary>
public interface IAdministratorDashboardService
{
    Task<AdministratorDashboardResponseDto> GetDashboardAsync(AdministratorDashboardFilterRequest filter);
}
