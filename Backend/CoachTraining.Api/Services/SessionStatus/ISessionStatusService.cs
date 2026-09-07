using SessionStatus = CoachTraining.Api.Models.Enums.SessionStatus;

namespace CoachTraining.Api.Services;

/// <summary>
/// Centralized Session Status transition rules (requirement.md 6.7, FR-STATUS-001–005,
/// todo.md 4.6) reused by every module that changes a TrainingSession's status —
/// Coach Teaching (4.7), Cancellation (4.12), Rescheduling (4.13), Approval/Locking (4.15).
/// Stateless; holds no data of its own.
/// </summary>
public interface ISessionStatusService
{
    /// <summary>True when moving from <paramref name="from"/> directly to <paramref name="to"/> is allowed.</summary>
    bool CanTransition(SessionStatus from, SessionStatus to);

    /// <summary>Throws with a Thai message when the transition is not allowed; no-op otherwise.</summary>
    void EnsureValidTransition(SessionStatus from, SessionStatus to);

    /// <summary>FR-STATUS-003 — Locked (and Submitted, pending review) records cannot be edited by a Coach.</summary>
    bool IsEditableByCoach(SessionStatus status);

    /// <summary>
    /// FR-SESSION-008, FR-STATUS-004/005 — whether a session in this status counts as
    /// completed teaching time: reached Completed or later, excluding Cancelled,
    /// Rescheduled(-original), and CoachAbsent (unless a substitute completed it,
    /// which is itself represented by the session reaching Completed).
    /// </summary>
    bool CountsAsCompletedTeaching(SessionStatus status);
}
