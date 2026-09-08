using CoachTraining.Api.DTOs.Cancellations;

namespace CoachTraining.Api.Services;

/// <summary>Cancellation operations for future or uncompleted sessions (FR-CR-001–003).</summary>
public interface ICancellationService
{
    Task<CancellationActionResult> CancelAsync(
        int trainingSessionId,
        CancelSessionRequest request,
        int actionByUserId);
}
