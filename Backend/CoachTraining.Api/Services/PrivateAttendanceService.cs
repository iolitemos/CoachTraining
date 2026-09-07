using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.PrivateAttendance;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class PrivateAttendanceService : IPrivateAttendanceService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<PrivateAttendanceService> _logger;

    public PrivateAttendanceService(ApplicationDbContext db, ISessionStatusService sessionStatusService, ILogger<PrivateAttendanceService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<PrivateAttendanceRosterResult?> GetRosterAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.PrivateAthletes)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

        if (session is null || session.TrainingType != TrainingType.Private)
        {
            return null;
        }

        if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
        {
            return null;
        }

        var attendanceByAthleteId = await _db.Attendances
            .Where(a => a.TrainingSessionId == trainingSessionId)
            .ToDictionaryAsync(a => a.AthleteId);

        return BuildRoster(session, attendanceByAthleteId);
    }

    public async Task<PrivateAttendanceActionResult> SetAsync(int trainingSessionId, int athleteId, PrivateAttendanceSetRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new PrivateAttendanceActionResult { NotFound = true };
            }

            if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
            {
                return new PrivateAttendanceActionResult { Forbidden = true };
            }

            if (session.TrainingType != TrainingType.Private)
            {
                return new PrivateAttendanceActionResult { Error = "เซสชันนี้ไม่ใช่การฝึกซ้อมส่วนตัว" };
            }

            if (!_sessionStatusService.IsEditableByCoach(session.Status))
            {
                return new PrivateAttendanceActionResult { Error = $"ไม่สามารถแก้ไขข้อมูลการเข้าร่วมได้ในสถานะปัจจุบัน ({session.Status})" };
            }

            // FR-PATT — attendance may only be recorded for an athlete actually
            // assigned to this Private session; the roster itself is fixed.
            var assignment = session.PrivateAthletes.FirstOrDefault(psa => psa.AthleteId == athleteId);
            if (assignment is null)
            {
                return new PrivateAttendanceActionResult { Error = "นักกีฬาคนนี้ไม่ได้ถูกกำหนดให้เข้าร่วมเซสชันนี้" };
            }

            var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.TrainingSessionId == trainingSessionId && a.AthleteId == athleteId);
            if (attendance is null)
            {
                attendance = new Attendance
                {
                    TrainingSessionId = trainingSessionId,
                    AthleteId = athleteId,
                    AthleteCodeSnapshot = assignment.AthleteCodeSnapshot,
                    AthleteNameSnapshot = assignment.AthleteNameSnapshot,
                    RecordedByUserId = actionByUserId,
                    RecordedDate = DateTime.UtcNow,
                };
                _db.Attendances.Add(attendance);
            }
            else
            {
                attendance.RecordedByUserId = actionByUserId;
                attendance.RecordedDate = DateTime.UtcNow;
            }

            attendance.Status = request.Status;
            attendance.ArrivalTime = request.Status == AttendanceStatus.Late ? request.ArrivalTime : null;
            attendance.Remark = request.Remark;

            await _db.SaveChangesAsync();

            var attendanceByAthleteId = await _db.Attendances
                .Where(a => a.TrainingSessionId == trainingSessionId)
                .ToDictionaryAsync(a => a.AthleteId);
            var rosterComplete = BuildRoster(session, attendanceByAthleteId).IsComplete;

            return new PrivateAttendanceActionResult
            {
                RosterComplete = rosterComplete,
                Attendance = new PrivateAttendanceRosterItemDto
                {
                    AthleteId = athleteId,
                    AthleteCode = assignment.AthleteCodeSnapshot,
                    FullName = assignment.AthleteNameSnapshot,
                    AttendanceId = attendance.AttendanceId,
                    Status = attendance.Status,
                    ArrivalTime = attendance.ArrivalTime,
                    Remark = attendance.Remark,
                    RecordedDate = attendance.RecordedDate,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set private attendance. Controller: PrivateAttendanceController Service: PrivateAttendanceService Function: SetAsync TrainingSessionId: {TrainingSessionId} AthleteId: {AthleteId} ActionByUserId: {ActionByUserId}", trainingSessionId, athleteId, actionByUserId);
            throw;
        }
    }

    private static PrivateAttendanceRosterResult BuildRoster(TrainingSession session, Dictionary<int, Attendance> attendanceByAthleteId)
    {
        var athletes = session.PrivateAthletes.Select(psa =>
        {
            attendanceByAthleteId.TryGetValue(psa.AthleteId, out var attendance);
            return new PrivateAttendanceRosterItemDto
            {
                AthleteId = psa.AthleteId,
                AthleteCode = psa.AthleteCodeSnapshot,
                FullName = psa.AthleteNameSnapshot,
                AttendanceId = attendance?.AttendanceId,
                Status = attendance?.Status,
                ArrivalTime = attendance?.ArrivalTime,
                Remark = attendance?.Remark,
                RecordedDate = attendance?.RecordedDate,
            };
        }).OrderBy(a => a.FullName).ToList();

        return new PrivateAttendanceRosterResult
        {
            Athletes = athletes,
            IsComplete = athletes.Count > 0 && athletes.All(a => a.Status is not null),
        };
    }
}
