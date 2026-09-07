namespace CoachTraining.Api.Models.Enums;

/// <summary>requirement.md FR-SESSION-002 — every session is exactly one of these.</summary>
public enum TrainingType
{
    Routine,
    Private,
}

/// <summary>requirement.md section 6.7 — Session Status Requirements.</summary>
public enum SessionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Submitted,
    Approved,
    Locked,
    Cancelled,
    Rescheduled,
    CoachAbsent,
}

/// <summary>
/// Union of Routine (Present, Late) and Private (Present, Absent, Late, Excused)
/// attendance statuses. Which subset is allowed per training type is enforced by
/// the Attendance services (todo.md 4.8/4.9), not by the database schema.
/// </summary>
public enum AttendanceStatus
{
    Present,
    Absent,
    Late,
    Excused,
}

/// <summary>requirement.md section 5.8 / FR-APPROVAL-* workflow actions.</summary>
public enum ApprovalActionType
{
    Submit,
    Approve,
    Reject,
    RequestRevision,
    Unlock,
}

/// <summary>requirement.md FR-CONFLICT-001–003.</summary>
public enum ConflictType
{
    CoachOverlap,
    AthleteOverlap,
    PrivateVsRoutineOverlap,
}
