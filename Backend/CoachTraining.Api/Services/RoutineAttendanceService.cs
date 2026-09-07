using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.RoutineAttendance;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class RoutineAttendanceService : IRoutineAttendanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<RoutineAttendanceService> _logger;

    public RoutineAttendanceService(ApplicationDbContext db, ISessionStatusService sessionStatusService, ILogger<RoutineAttendanceService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<List<RoutineAttendanceListItemDto>?> ListAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions.FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);
        if (session is null || session.TrainingType != TrainingType.Routine)
        {
            return null;
        }

        if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
        {
            return null;
        }

        return await _db.Attendances
            .Where(a => a.TrainingSessionId == trainingSessionId)
            .OrderBy(a => a.AthleteNameSnapshot)
            .Select(a => MapToListItem(a))
            .ToListAsync();
    }

    public async Task<RoutineAttendanceActionResult> AddAsync(int trainingSessionId, RoutineAttendanceCreateDto dto, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var (session, errorResult) = await LoadEditableRoutineSessionAsync(trainingSessionId, isPrivilegedRole, currentCoachId);
            if (errorResult is not null)
            {
                return errorResult;
            }

            // Defense in depth alongside the DTO's own IValidatableObject check.
            if (dto.Status is not (AttendanceStatus.Present or AttendanceStatus.Late))
            {
                return new RoutineAttendanceActionResult { Error = "การฝึกซ้อมประจำอนุญาตเฉพาะสถานะ มาเรียน หรือ มาสาย เท่านั้น" };
            }

            var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == dto.AthleteId);
            if (athlete is null)
            {
                return new RoutineAttendanceActionResult { Error = "ไม่พบข้อมูลนักกีฬาที่เลือก" };
            }

            if (!athlete.IsActive)
            {
                return new RoutineAttendanceActionResult { Error = "ไม่สามารถบันทึกการเข้าร่วมให้นักกีฬาที่ปิดใช้งานได้" };
            }

            // FR-RATT-004 — no duplicate attendance for the same athlete/session.
            var alreadyRecorded = await _db.Attendances.AnyAsync(a => a.TrainingSessionId == trainingSessionId && a.AthleteId == dto.AthleteId);
            if (alreadyRecorded)
            {
                return new RoutineAttendanceActionResult { Error = "นักกีฬาคนนี้ถูกบันทึกการเข้าร่วมแล้ว" };
            }

            var attendance = new Attendance
            {
                TrainingSessionId = trainingSessionId,
                AthleteId = athlete.AthleteId,
                AthleteCodeSnapshot = athlete.AthleteCode,
                AthleteNameSnapshot = athlete.FullName,
                Status = dto.Status,
                ArrivalTime = dto.Status == AttendanceStatus.Late ? dto.ArrivalTime : null,
                Remark = dto.Remark,
                RecordedByUserId = actionByUserId,
                RecordedDate = DateTime.UtcNow,
            };

            _db.Attendances.Add(attendance);
            await _db.SaveChangesAsync();

            return new RoutineAttendanceActionResult { Success = true, Attendance = MapToListItem(attendance) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add routine attendance. Controller: RoutineAttendanceController Service: RoutineAttendanceService Function: AddAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    public async Task<RoutineAttendanceActionResult> UpdateAsync(int trainingSessionId, int attendanceId, RoutineAttendanceUpdateDto dto, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        var (session, errorResult) = await LoadEditableRoutineSessionAsync(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (errorResult is not null)
        {
            return errorResult;
        }

        if (dto.Status is not (AttendanceStatus.Present or AttendanceStatus.Late))
        {
            return new RoutineAttendanceActionResult { Error = "การฝึกซ้อมประจำอนุญาตเฉพาะสถานะ มาเรียน หรือ มาสาย เท่านั้น" };
        }

        var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.AttendanceId == attendanceId && a.TrainingSessionId == trainingSessionId);
        if (attendance is null)
        {
            return new RoutineAttendanceActionResult { NotFound = true };
        }

        attendance.Status = dto.Status;
        attendance.ArrivalTime = dto.Status == AttendanceStatus.Late ? dto.ArrivalTime : null;
        attendance.Remark = dto.Remark;

        await _db.SaveChangesAsync();

        return new RoutineAttendanceActionResult { Success = true, Attendance = MapToListItem(attendance) };
    }

    public async Task<RoutineAttendanceActionResult> RemoveAsync(int trainingSessionId, int attendanceId, bool isPrivilegedRole, int? currentCoachId)
    {
        var (session, errorResult) = await LoadEditableRoutineSessionAsync(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (errorResult is not null)
        {
            return errorResult;
        }

        var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.AttendanceId == attendanceId && a.TrainingSessionId == trainingSessionId);
        if (attendance is null)
        {
            return new RoutineAttendanceActionResult { NotFound = true };
        }

        _db.Attendances.Remove(attendance);
        await _db.SaveChangesAsync();

        return new RoutineAttendanceActionResult { Success = true };
    }

    private async Task<(TrainingSession? Session, RoutineAttendanceActionResult? ErrorResult)> LoadEditableRoutineSessionAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions.FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);
        if (session is null)
        {
            return (null, new RoutineAttendanceActionResult { NotFound = true });
        }

        if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
        {
            return (null, new RoutineAttendanceActionResult { Forbidden = true });
        }

        if (session.TrainingType != TrainingType.Routine)
        {
            return (null, new RoutineAttendanceActionResult { Error = "เซสชันนี้ไม่ใช่การฝึกซ้อมประจำ" });
        }

        if (!_sessionStatusService.IsEditableByCoach(session.Status))
        {
            return (null, new RoutineAttendanceActionResult { Error = $"ไม่สามารถแก้ไขข้อมูลการเข้าร่วมได้ในสถานะปัจจุบัน ({session.Status})" });
        }

        return (session, null);
    }

    private static RoutineAttendanceListItemDto MapToListItem(Attendance attendance) => new()
    {
        AttendanceId = attendance.AttendanceId,
        AthleteId = attendance.AthleteId,
        AthleteCode = attendance.AthleteCodeSnapshot,
        FullName = attendance.AthleteNameSnapshot,
        Status = attendance.Status,
        ArrivalTime = attendance.ArrivalTime,
        Remark = attendance.Remark,
        RecordedDate = attendance.RecordedDate,
    };
}
