using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;

namespace CoachTraining.Api.Services;

public interface ICoachService
{
    Task<PagedResponse<CoachListItemDto>> ListAsync(PagedRequest request);
    Task<CoachDetailDto?> GetByIdAsync(int coachId);
    Task<(CoachDetailDto? Result, string? Error)> CreateAsync(CoachCreateDto dto, int actionByUserId);
    Task<(CoachDetailDto? Result, string? Error)> UpdateAsync(int coachId, CoachUpdateDto dto, int actionByUserId);
    Task<bool> SetStatusAsync(int coachId, bool isActive, int actionByUserId);

    /// <summary>Active coaches only — used by dropdowns (User-Coach linking, future Routine/Private Training).</summary>
    Task<List<CoachOptionDto>> GetActiveOptionsAsync();
}
