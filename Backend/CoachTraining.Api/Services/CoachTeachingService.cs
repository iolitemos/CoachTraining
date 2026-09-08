using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.CoachTeaching;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class CoachTeachingService : ICoachTeachingService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<CoachTeachingService> _logger;

    public CoachTeachingService(ApplicationDbContext db, ISessionStatusService sessionStatusService, ILogger<CoachTeachingService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<TeachingActionResult> StartAsync(int trainingSessionId, TeachingStartRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions.Include(s => s.PrivateAthletes).Include(s => s.TrainingLog).FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);
            if (session is null)
            {
                return new TeachingActionResult { NotFound = true };
            }

            if (!IsAuthorizedForSession(session.AssignedCoachId, session.ActualCoachId, isPrivilegedRole, currentCoachId))
            {
                return new TeachingActionResult { Forbidden = true };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.InProgress))
            {
                return new TeachingActionResult { Error = $"ไม่สามารถเริ่มฝึกซ้อมได้ในสถานะปัจจุบัน ({session.Status})" };
            }

            // FR-TEACH-005 / FR-SUB-003–004 — preserve an actual coach already
            // designated by the substitution workflow. Otherwise the signed-in
            // assigned Coach becomes actual; a pure Administrator acts on behalf
            // of the original assignment.
            if (session.ActualCoachId is null && currentCoachId is not null)
            {
                var actingCoach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == currentCoachId);
                if (actingCoach is null)
                {
                    return new TeachingActionResult { Error = "ไม่พบข้อมูลโค้ชของบัญชีนี้" };
                }

                session.ActualCoachId = actingCoach.CoachId;
                session.ActualCoachCodeSnapshot = actingCoach.CoachCode;
                session.ActualCoachNameSnapshot = actingCoach.FullName;
            }
            else if (session.ActualCoachId is null)
            {
                session.ActualCoachId = session.AssignedCoachId;
                session.ActualCoachCodeSnapshot = session.AssignedCoachCodeSnapshot;
                session.ActualCoachNameSnapshot = session.AssignedCoachNameSnapshot;
            }

            // FR-TEACH-004 — scheduled values are never touched here.
            session.ActualStartDateTime = request.ActualStartDateTime ?? DateTime.Now;
            session.Status = SessionStatus.InProgress;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new TeachingActionResult { Session = TrainingSessionMapper.ToDetailDto(session) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start teaching. Controller: CoachTeachingController Service: CoachTeachingService Function: StartAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    public async Task<TeachingActionResult> CompleteAsync(int trainingSessionId, TeachingEndRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions.Include(s => s.PrivateAthletes).Include(s => s.TrainingLog).FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);
            if (session is null)
            {
                return new TeachingActionResult { NotFound = true };
            }

            if (!IsAuthorizedForSession(session.AssignedCoachId, session.ActualCoachId, isPrivilegedRole, currentCoachId))
            {
                return new TeachingActionResult { Forbidden = true };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Completed))
            {
                return new TeachingActionResult { Error = $"ต้องเริ่มฝึกซ้อมก่อนจึงจะบันทึกการเสร็จสิ้นได้ (สถานะปัจจุบัน: {session.Status})" };
            }

            var actualEnd = request.ActualEndDateTime ?? DateTime.Now;

            // FR-SESSION-007 / "reject negative or invalid actual duration". ActualStartDateTime
            // is always set at this point — Completed is only reachable from InProgress.
            if (actualEnd <= session.ActualStartDateTime!.Value)
            {
                return new TeachingActionResult { Error = "เวลาสิ้นสุดต้องอยู่หลังเวลาเริ่มฝึกซ้อมจริง" };
            }

            session.ActualEndDateTime = actualEnd;
            session.Status = SessionStatus.Completed;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            // Validating attendance/training-log completeness before completion happens
            // once those modules (todo.md 4.8–4.10) exist; FR-TEACH-006's submission-time
            // completeness check belongs to the Approval module (4.15).
            await _db.SaveChangesAsync();

            return new TeachingActionResult { Session = TrainingSessionMapper.ToDetailDto(session) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete teaching. Controller: CoachTeachingController Service: CoachTeachingService Function: CompleteAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    private static bool IsAuthorizedForSession(int assignedCoachId, int? actualCoachId, bool isPrivilegedRole, int? currentCoachId) =>
        isPrivilegedRole || (actualCoachId ?? assignedCoachId) == currentCoachId;
}
