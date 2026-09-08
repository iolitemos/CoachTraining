using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;

namespace CoachTraining.Api.Services;

/// <summary>
/// Unified read layer over TrainingSession (requirement.md 4.6, todo.md 4.5) — the
/// shared query surface Coach Home, Administrator Dashboard, and Reports will sit on.
/// </summary>
public interface ITrainingSessionService
{
    /// <summary>
    /// When <paramref name="isPrivilegedRole"/> is false (a Coach account), results are
    /// always restricted to <paramref name="currentCoachId"/> regardless of any CoachId
    /// filter supplied — an unlinked Coach account (<paramref name="currentCoachId"/> null)
    /// sees nothing rather than everything (CLAUDE.md section 11).
    /// </summary>
    Task<PagedResponse<TrainingSessionListItemDto>> ListAsync(TrainingSessionFilterRequest filter, bool isPrivilegedRole, int? currentCoachId);

    /// <summary>Returns null both when the session doesn't exist and when a Coach requests one that isn't theirs.</summary>
    Task<TrainingSessionDetailDto?> GetByIdAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);
    Task<(TrainingSessionDetailDto? Session, string? Error, bool NotFound)> ResetToScheduledAsync(int trainingSessionId, string reason, int actionByUserId);
}
