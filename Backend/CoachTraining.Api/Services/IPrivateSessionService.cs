using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.PrivateSessions;

namespace CoachTraining.Api.Services;

public interface IPrivateSessionService
{
    Task<PagedResponse<PrivateSessionListItemDto>> ListAsync(PagedRequest request);
    Task<PrivateSessionDetailDto?> GetByIdAsync(int trainingSessionId);
    Task<PrivateSessionSaveResult> CreateAsync(PrivateSessionCreateDto dto, int actionByUserId);
    Task<PrivateSessionSaveResult> UpdateAsync(int trainingSessionId, PrivateSessionUpdateDto dto, int actionByUserId);
}
