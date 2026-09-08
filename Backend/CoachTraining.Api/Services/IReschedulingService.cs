using CoachTraining.Api.DTOs.Reschedules;

namespace CoachTraining.Api.Services;

/// <summary>Rescheduling for eligible sessions (requirement.md 6.13, FR-CR-004–007, todo.md 4.13).</summary>
public interface IReschedulingService
{
    Task<RescheduleActionResult> RescheduleAsync(int trainingSessionId, RescheduleSessionRequest request, int actionByUserId);
}
