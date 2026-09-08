using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Approvals;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Submit/Approve/Reject/RequestRevision/Unlock workflow (requirement.md 6.15, todo.md 4.15).</summary>
public class TrainingApprovalService : ITrainingApprovalService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<TrainingApprovalService> _logger;

    public TrainingApprovalService(ApplicationDbContext db, ISessionStatusService sessionStatusService, ILogger<TrainingApprovalService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<ApprovalActionResult> SubmitAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new ApprovalActionResult { NotFound = true };
            }

            if (!IsAuthorizedForSession(session.AssignedCoachId, session.ActualCoachId, isPrivilegedRole, currentCoachId))
            {
                return new ApprovalActionResult { Forbidden = true };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Submitted))
            {
                return new ApprovalActionResult { Error = $"ต้องบันทึกการเสร็จสิ้นฝึกซ้อมก่อนจึงจะส่งตรวจได้ (สถานะปัจจุบัน: {session.Status})" };
            }

            // FR-TEACH-006 / FR-PATT-002: a Coach shall not submit an incomplete
            // session record — every athlete assigned to a Private Training
            // session must already have an attendance status recorded.
            if (session.TrainingType == TrainingType.Private)
            {
                var recordedCount = await _db.Attendances.CountAsync(a => a.TrainingSessionId == trainingSessionId);
                if (session.PrivateAthletes.Count == 0 || recordedCount < session.PrivateAthletes.Count)
                {
                    return new ApprovalActionResult { Error = "กรุณาบันทึกสถานะการเข้าร่วมของนักกีฬาทุกคนก่อนส่งตรวจ" };
                }
            }

            session.Status = SessionStatus.Submitted;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            var history = AddHistory(trainingSessionId, ApprovalActionType.Submit, reason: null, actionByUserId);
            await _db.SaveChangesAsync();

            return new ApprovalActionResult { Session = TrainingSessionMapper.ToDetailDto(session), History = ToHistoryDto(history) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit training session. Controller: TrainingApprovalsController Service: TrainingApprovalService Function: SubmitAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    public async Task<ApprovalActionResult> ApproveAsync(int trainingSessionId, ApprovalCommentRequest request, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new ApprovalActionResult { NotFound = true };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Approved))
            {
                return new ApprovalActionResult { Error = $"ไม่สามารถอนุมัติเซสชันในสถานะปัจจุบัน ({session.Status}) ได้" };
            }

            // FR-APPROVAL-003: approved records become Locked immediately —
            // requirement.md defines no separate "Lock" action, so the single
            // Approve action performs both status transitions (Submitted ->
            // Approved -> Locked) and is recorded once as ApprovalActionType.Approve.
            _sessionStatusService.EnsureValidTransition(SessionStatus.Approved, SessionStatus.Locked);
            session.Status = SessionStatus.Locked;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            var history = AddHistory(trainingSessionId, ApprovalActionType.Approve, request.Reason?.Trim(), actionByUserId);
            await _db.SaveChangesAsync();

            return new ApprovalActionResult { Session = TrainingSessionMapper.ToDetailDto(session), History = ToHistoryDto(history) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve training session. Controller: TrainingApprovalsController Service: TrainingApprovalService Function: ApproveAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    public Task<ApprovalActionResult> RejectAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId) =>
        ReturnToCompletedAsync(trainingSessionId, ApprovalActionType.Reject, request, actionByUserId, "ไม่สามารถตีกลับเซสชันในสถานะปัจจุบัน");

    public Task<ApprovalActionResult> RequestRevisionAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId) =>
        ReturnToCompletedAsync(trainingSessionId, ApprovalActionType.RequestRevision, request, actionByUserId, "ไม่สามารถขอแก้ไขเซสชันในสถานะปัจจุบัน");

    public async Task<ApprovalActionResult> UnlockAsync(int trainingSessionId, ApprovalReasonRequest request, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new ApprovalActionResult { NotFound = true };
            }

            // FR-APPROVAL-005/006: only a Locked record can be unlocked, and only
            // with a reason — enforced by [Required] on ApprovalReasonRequest and
            // re-checked here per backend validation standards.
            var reason = request.Reason.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ApprovalActionResult { Error = "กรุณาระบุเหตุผลในการปลดล็อก" };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Completed))
            {
                return new ApprovalActionResult { Error = $"สามารถปลดล็อกได้เฉพาะเซสชันที่ถูกล็อกแล้วเท่านั้น (สถานะปัจจุบัน: {session.Status})" };
            }

            session.Status = SessionStatus.Completed;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            var history = AddHistory(trainingSessionId, ApprovalActionType.Unlock, reason, actionByUserId);
            await _db.SaveChangesAsync();

            return new ApprovalActionResult { Session = TrainingSessionMapper.ToDetailDto(session), History = ToHistoryDto(history) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlock training session. Controller: TrainingApprovalsController Service: TrainingApprovalService Function: UnlockAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    /// <summary>Shared implementation for Reject and Request Revision — both return a
    /// Submitted record to Completed for correction and resubmission, differing only
    /// in the recorded ApprovalActionType (see SessionStatusService design note).</summary>
    private async Task<ApprovalActionResult> ReturnToCompletedAsync(
        int trainingSessionId, ApprovalActionType actionType, ApprovalReasonRequest request, int actionByUserId, string ineligibleMessagePrefix)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new ApprovalActionResult { NotFound = true };
            }

            var reason = request.Reason.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ApprovalActionResult { Error = "กรุณาระบุเหตุผล" };
            }

            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Completed))
            {
                return new ApprovalActionResult { Error = $"{ineligibleMessagePrefix} ({session.Status})" };
            }

            session.Status = SessionStatus.Completed;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = DateTime.UtcNow;

            var history = AddHistory(trainingSessionId, actionType, reason, actionByUserId);
            await _db.SaveChangesAsync();

            return new ApprovalActionResult { Session = TrainingSessionMapper.ToDetailDto(session), History = ToHistoryDto(history) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record approval action. Controller: TrainingApprovalsController Service: TrainingApprovalService Function: ReturnToCompletedAsync ActionType: {ActionType} TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", actionType, trainingSessionId, actionByUserId);
            throw;
        }
    }

    private TrainingApprovalHistory AddHistory(int trainingSessionId, ApprovalActionType actionType, string? reason, int actionByUserId)
    {
        var history = new TrainingApprovalHistory
        {
            TrainingSessionId = trainingSessionId,
            ActionType = actionType,
            Reason = reason,
            ActionByUserId = actionByUserId,
            ActionDate = DateTime.UtcNow,
        };
        _db.TrainingApprovalHistories.Add(history);
        return history;
    }

    private static TrainingApprovalHistoryDto ToHistoryDto(TrainingApprovalHistory history) => new()
    {
        TrainingApprovalHistoryId = history.TrainingApprovalHistoryId,
        TrainingSessionId = history.TrainingSessionId,
        ActionType = history.ActionType,
        Reason = history.Reason,
        ActionByUserId = history.ActionByUserId,
        ActionDate = history.ActionDate,
    };

    private static bool IsAuthorizedForSession(int assignedCoachId, int? actualCoachId, bool isPrivilegedRole, int? currentCoachId) =>
        isPrivilegedRole || (actualCoachId ?? assignedCoachId) == currentCoachId;
}
