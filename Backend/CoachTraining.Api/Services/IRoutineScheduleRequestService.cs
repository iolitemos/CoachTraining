using CoachTraining.Api.DTOs.RoutineScheduleRequests;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Services;

public interface IRoutineScheduleRequestService
{
    Task<RoutineScheduleRequestActionResult> CreateAsync(int coachId, RoutineScheduleRequestCreateDto dto, int userId);
    Task<(List<RoutineScheduleRequestDto> Requests, string? Error, List<string> Conflicts)> CreateBatchAsync(int coachId, RoutineScheduleRequestBatchCreateDto dto, int userId);
    Task<List<RoutineScheduleRequestDto>> ListOwnAsync(int coachId, DateOnly? dateFrom = null, DateOnly? dateTo = null);
    Task<RoutineScheduleRequestActionResult> UpdateOwnAsync(int id, int coachId, RoutineScheduleRequestCreateDto dto, int userId);
    Task<RoutineScheduleRequestActionResult> CancelOwnAsync(int id, int coachId, int userId);
    Task<List<RoutineScheduleRequestDto>> ListAsync(RoutineScheduleRequestStatus? status);
    Task<RoutineScheduleRequestActionResult> ApproveAsync(int id, RoutineScheduleRequestReviewDto dto, int userId);
    Task<RoutineScheduleRequestBatchActionResult> ApproveBatchAsync(RoutineScheduleRequestBatchApproveDto dto, int userId);
    Task<RoutineScheduleRequestActionResult> RequestRevisionAsync(int id, RoutineScheduleRequestDecisionDto dto, int userId);
    Task<RoutineScheduleRequestActionResult> RejectAsync(int id, RoutineScheduleRequestDecisionDto dto, int userId);
    Task<RoutineScheduleRequestActionResult> DeleteApprovedAsync(int id, int userId);
}
