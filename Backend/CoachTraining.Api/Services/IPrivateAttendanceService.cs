using CoachTraining.Api.DTOs.PrivateAttendance;

namespace CoachTraining.Api.Services;

/// <summary>
/// Private Attendance (requirement.md 4.8/6.10, todo.md 4.9). The roster is fixed by
/// the session's assigned athletes (PrivateSessionAthlete) — attendance can only be
/// recorded for athletes actually assigned, and the roster always lists everyone so
/// missing entries are identifiable (FR-PATT-001/002).
/// </summary>
public interface IPrivateAttendanceService
{
    /// <summary>Null means the session doesn't exist, isn't Private, or isn't this Coach's.</summary>
    Task<PrivateAttendanceRosterResult?> GetRosterAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    /// <summary>Creates or updates the attendance record for one already-assigned athlete.</summary>
    Task<PrivateAttendanceActionResult> SetAsync(int trainingSessionId, int privateSessionAthleteId, PrivateAttendanceSetRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);
}
