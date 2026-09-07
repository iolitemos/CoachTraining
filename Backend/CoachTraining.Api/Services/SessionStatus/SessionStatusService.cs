using SessionStatus = CoachTraining.Api.Models.Enums.SessionStatus;

namespace CoachTraining.Api.Services;

public class SessionStatusService : ISessionStatusService
{
    /// <summary>
    /// The full status graph (requirement.md 6.7 / section 5.8 business flow).
    /// Design note on Reject / Request Revision (FR-APPROVAL-002): the canonical
    /// status list in requirement.md has no separate "Rejected"/"RevisionRequested"
    /// status, so both actions return a Submitted record to Completed — editable by
    /// the Coach again — while the distinct action and reason are recorded in
    /// TrainingApprovalHistory (ApprovalActionType), not as a new session status.
    /// Unlock (FR-APPROVAL-005) similarly returns a Locked record to Completed for
    /// correction and resubmission.
    /// </summary>
    private static readonly Dictionary<SessionStatus, SessionStatus[]> AllowedTransitions = new()
    {
        [SessionStatus.Scheduled] = [SessionStatus.InProgress, SessionStatus.Cancelled, SessionStatus.Rescheduled, SessionStatus.CoachAbsent],
        [SessionStatus.InProgress] = [SessionStatus.Completed, SessionStatus.Cancelled],
        [SessionStatus.CoachAbsent] = [SessionStatus.InProgress, SessionStatus.Cancelled, SessionStatus.Rescheduled],
        [SessionStatus.Completed] = [SessionStatus.Submitted],
        [SessionStatus.Submitted] = [SessionStatus.Approved, SessionStatus.Completed],
        [SessionStatus.Approved] = [SessionStatus.Locked],
        [SessionStatus.Locked] = [SessionStatus.Completed],
        [SessionStatus.Cancelled] = [SessionStatus.Scheduled],
        [SessionStatus.Rescheduled] = [],
    };

    /// <summary>Statuses a Coach may still record data against / act on.</summary>
    private static readonly HashSet<SessionStatus> CoachEditableStatuses = [SessionStatus.Scheduled, SessionStatus.InProgress, SessionStatus.Completed];

    /// <summary>Statuses that represent completed teaching time.</summary>
    private static readonly HashSet<SessionStatus> CompletedTeachingStatuses = [SessionStatus.Completed, SessionStatus.Submitted, SessionStatus.Approved, SessionStatus.Locked];

    public bool CanTransition(SessionStatus from, SessionStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public void EnsureValidTransition(SessionStatus from, SessionStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"ไม่สามารถเปลี่ยนสถานะจาก {from} เป็น {to} ได้");
        }
    }

    public bool IsEditableByCoach(SessionStatus status) => CoachEditableStatuses.Contains(status);

    public bool CountsAsCompletedTeaching(SessionStatus status) => CompletedTeachingStatuses.Contains(status);
}
