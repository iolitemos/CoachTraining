using CoachTraining.Api.DTOs.Approvals;

namespace CoachTraining.Api.Services;

/// <summary>
/// Submit/Approve/Reject/RequestRevision/Unlock workflow (requirement.md 6.15,
/// FR-APPROVAL-001–007, todo.md 4.15). Every action is recorded in
/// TrainingApprovalHistory (FR-APPROVAL-007).
/// </summary>
public interface ITrainingApprovalService
{
    /// <summary>FR-APPROVAL-001 — Coach (or Administrator on their behalf) submits a
    /// Completed record for review. Blocks submission when required session
    /// information is missing (FR-TEACH-006, FR-PATT-002).</summary>
    Task<ApprovalActionResult> SubmitAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);

    /// <summary>FR-APPROVAL-002/003 — Administrator approves a submitted record, which
    /// becomes Locked immediately (there is no separate "Lock" action).</summary>
    Task<ApprovalActionResult> ApproveAsync(int trainingSessionId, ApprovalCommentRequest request, int actionByUserId);

    /// <summary>FR-APPROVAL-002 — returns the record to Completed for correction and resubmission.</summary>
    Task<ApprovalActionResult> RejectAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId);

    /// <summary>FR-APPROVAL-002 — same effect as Reject, recorded as a distinct action type.</summary>
    Task<ApprovalActionResult> RequestRevisionAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId);

    /// <summary>FR-APPROVAL-005/006 — authorized Administrator unlocks a Locked record for correction.</summary>
    Task<ApprovalActionResult> UnlockAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId);
}
