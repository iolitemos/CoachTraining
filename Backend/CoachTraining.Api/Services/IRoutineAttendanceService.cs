using CoachTraining.Api.DTOs.RoutineAttendance;

namespace CoachTraining.Api.Services;

/// <summary>
/// Routine Attendance (requirement.md 4.8/6.9, todo.md 4.8). No pre-assigned roster —
/// only athletes the Coach actively selects get a record; unselected athletes are
/// never inferred absent (FR-RATT-002/003, CLAUDE.md 4.2).
/// </summary>
public interface IRoutineAttendanceService
{
    /// <summary>Null means the session doesn't exist, isn't Routine, or isn't this Coach's.</summary>
    Task<List<RoutineAttendanceListItemDto>?> ListAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    Task<RoutineAttendanceActionResult> AddAsync(int trainingSessionId, RoutineAttendanceCreateDto dto, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);

    Task<RoutineAttendanceActionResult> UpdateAsync(int trainingSessionId, int attendanceId, RoutineAttendanceUpdateDto dto, bool isPrivilegedRole, int? currentCoachId, int actionByUserId);

    Task<RoutineAttendanceActionResult> RemoveAsync(int trainingSessionId, int attendanceId, bool isPrivilegedRole, int? currentCoachId);
}
