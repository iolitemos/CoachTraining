using CoachTraining.Api.DTOs.CoachTeaching;

namespace CoachTraining.Api.Services;

/// <summary>Coach Teaching Record (requirement.md 4.7, todo.md 4.7, FR-TEACH-001–006).</summary>
public interface ICoachTeachingService
{
    /// <summary>
    /// Records actual teaching start and moves the session to InProgress. Allowed
    /// for the session's assigned/actual coach or an Administrator (correction).
    /// </summary>
    Task<TeachingActionResult> StartAsync(int trainingSessionId, TeachingStartRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);

    /// <summary>Records actual teaching end, computes duration, and moves the session to Completed.</summary>
    Task<TeachingActionResult> CompleteAsync(int trainingSessionId, TeachingEndRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);
}
