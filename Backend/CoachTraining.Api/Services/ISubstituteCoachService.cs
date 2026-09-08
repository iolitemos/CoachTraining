using CoachTraining.Api.DTOs.Substitutions;

namespace CoachTraining.Api.Services;

/// <summary>Substitute Coach Management (requirement.md 4.10, FR-SUB-001–005).</summary>
public interface ISubstituteCoachService
{
    Task<SubstituteCoachActionResult> AssignAsync(
        int trainingSessionId,
        SubstituteCoachRequest request,
        int actionByUserId);
}
