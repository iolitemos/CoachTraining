using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.RoutineSchedules;

namespace CoachTraining.Api.Services;

public interface IRoutineScheduleService
{
    Task<PagedResponse<RoutineScheduleListItemDto>> ListAsync(PagedRequest request);
    Task<RoutineScheduleDetailDto?> GetByIdAsync(int routineScheduleId);
    Task<RoutineScheduleSaveResult> CreateAsync(RoutineScheduleCreateDto dto, int actionByUserId);
    Task<RoutineScheduleSaveResult> UpdateAsync(int routineScheduleId, RoutineScheduleUpdateDto dto, int actionByUserId);
    Task<(bool Found, string? Error)> DeleteAsync(int routineScheduleId, int actionByUserId);
    Task<bool> SetStatusAsync(int routineScheduleId, bool isActive, int actionByUserId);

    /// <summary>Extends session generation for an existing schedule. Null result + null error means not found.</summary>
    Task<(GenerateSessionsResult? Result, string? Error)> GenerateSessionsAsync(int routineScheduleId, DateOnly throughDate, int actionByUserId);
}
