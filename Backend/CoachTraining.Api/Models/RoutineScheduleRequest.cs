using CoachTraining.Api.Models.Common;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Models;

/// <summary>A Coach proposal for Routine Training. No operational schedule/session exists until approval.</summary>
public class RoutineScheduleRequest : AuditableEntity
{
    public int RoutineScheduleRequestId { get; set; }
    public Guid SubmissionBatchId { get; set; }
    public int CoachId { get; set; }
    public Coach Coach { get; set; } = null!;
    public DateOnly ProposedDate { get; set; }
    public TimeOnly ProposedStartTime { get; set; }
    public TimeOnly ProposedEndTime { get; set; }
    public string? ProposedRemarks { get; set; }
    public RoutineScheduleRequestStatus Status { get; set; } = RoutineScheduleRequestStatus.Pending;

    public DateOnly? ApprovedDate { get; set; }
    public TimeOnly? ApprovedStartTime { get; set; }
    public TimeOnly? ApprovedEndTime { get; set; }
    public string? ApprovedRemarks { get; set; }
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewReason { get; set; }

    public int? RoutineScheduleId { get; set; }
    public RoutineSchedule? RoutineSchedule { get; set; }
}
