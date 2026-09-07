using CoachTraining.Api.DTOs.TrainingLogs;

namespace CoachTraining.Api.Services;

/// <summary>Training Log (requirement.md 4.9, todo.md 4.10, FR-LOG-001–004). Applies to any Training Session.</summary>
public interface ITrainingLogService
{
    /// <summary>Null means the session doesn't exist or isn't this Coach's; a session with no saved log
    /// still returns a DTO whose TrainingLogId is null.</summary>
    Task<TrainingLogDto?> GetAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    Task<TrainingLogActionResult> UpsertAsync(int trainingSessionId, TrainingLogUpsertRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);
}
