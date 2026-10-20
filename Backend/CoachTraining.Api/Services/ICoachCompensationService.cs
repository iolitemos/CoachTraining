using CoachTraining.Api.DTOs.Compensations;

namespace CoachTraining.Api.Services;

public interface ICoachCompensationService
{
    Task<CoachCompensationStatementDto?> GetAsync(int coachId, int year, int month);
    Task<CoachCompensationActionResult> CalculateAsync(CoachCompensationPeriodRequest request, int actionByUserId);
    Task<CoachCompensationActionResult> UpdateAdjustmentAsync(int statementId, CoachCompensationAdjustmentRequest request, int actionByUserId);
    Task<CoachCompensationActionResult> ConfirmAsync(int statementId, int actionByUserId);
    Task<CoachCompensationActionResult> MarkPaidAsync(int statementId, int actionByUserId);
}
