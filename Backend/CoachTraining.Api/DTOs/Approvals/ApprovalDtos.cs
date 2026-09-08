using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.Approvals;

/// <summary>Approve action body — a comment is optional (FR-APPROVAL-002/007).</summary>
public class ApprovalCommentRequest
{
    [MaxLength(1000, ErrorMessage = "ความคิดเห็นต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string? Reason { get; set; }
}

/// <summary>Reject, Request Revision, and Unlock action body — a reason is required
/// (FR-APPROVAL-002, FR-APPROVAL-006).</summary>
public class ApprovalReasonRequest
{
    [Required(ErrorMessage = "กรุณาระบุเหตุผล")]
    [MaxLength(1000, ErrorMessage = "เหตุผลต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Immutable business details recorded for one approval workflow action.</summary>
public class TrainingApprovalHistoryDto
{
    public int TrainingApprovalHistoryId { get; set; }
    public int TrainingSessionId { get; set; }
    public ApprovalActionType ActionType { get; set; }
    public string? Reason { get; set; }
    public int ActionByUserId { get; set; }
    public DateTime ActionDate { get; set; }
}

/// <summary>Service-layer result translated to HTTP semantics by the thin controller.</summary>
public class ApprovalActionResult
{
    public TrainingSessionDetailDto? Session { get; set; }
    public TrainingApprovalHistoryDto? History { get; set; }
    public string? Error { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
}

/// <summary>API response body for a successful approval workflow action.</summary>
public class TrainingApprovalActionResponseDto
{
    public TrainingSessionDetailDto Session { get; set; } = null!;
    public TrainingApprovalHistoryDto History { get; set; } = null!;
}
