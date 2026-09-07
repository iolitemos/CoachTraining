using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>Submit/Approve/Reject/RequestRevision/Unlock audit trail (requirement.md 5.8, FR-APPROVAL-007).</summary>
public class TrainingApprovalHistory
{
    public int TrainingApprovalHistoryId { get; set; }

    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;

    public ApprovalActionType ActionType { get; set; }

    /// <summary>Required for Reject, Request Revision, and Unlock actions (FR-APPROVAL-006).</summary>
    public string? Reason { get; set; }

    public int ActionByUserId { get; set; }
    public User ActionByUser { get; set; } = null!;

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
