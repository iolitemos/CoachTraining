using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Substitutions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class SubstituteCoachService : ISubstituteCoachService
{
    private readonly ApplicationDbContext _db;
    private readonly IScheduleConflictService _conflictService;
    private readonly ILogger<SubstituteCoachService> _logger;

    public SubstituteCoachService(
        ApplicationDbContext db,
        IScheduleConflictService conflictService,
        ILogger<SubstituteCoachService> logger)
    {
        _db = db;
        _conflictService = conflictService;
        _logger = logger;
    }

    public async Task<SubstituteCoachActionResult> AssignAsync(
        int trainingSessionId,
        SubstituteCoachRequest request,
        int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .Include(s => s.TrainingLog)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new SubstituteCoachActionResult { NotFound = true };
            }

            // The requirement flow assigns a substitute before the session is
            // taught. CoachAbsent remains eligible so an Administrator can recover
            // that session by assigning the coach who will actually teach it.
            if (session.Status is not (SessionStatus.Scheduled or SessionStatus.CoachAbsent))
            {
                return new SubstituteCoachActionResult
                {
                    Error = "สามารถกำหนดโค้ชตัวแทนได้เฉพาะเซสชันที่ยังไม่เริ่มหรือมีสถานะโค้ชขาดเท่านั้น",
                };
            }

            var reason = request.Reason.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new SubstituteCoachActionResult { Error = "กรุณาระบุเหตุผลในการเปลี่ยนโค้ช" };
            }

            if (request.SubstituteCoachId == session.AssignedCoachId)
            {
                return new SubstituteCoachActionResult { Error = "โค้ชตัวแทนต้องไม่ใช่โค้ชที่ได้รับมอบหมายเดิม" };
            }

            var substituteCoach = await _db.Coaches
                .FirstOrDefaultAsync(c => c.CoachId == request.SubstituteCoachId);

            if (substituteCoach is null)
            {
                return new SubstituteCoachActionResult { Error = "ไม่พบข้อมูลโค้ชตัวแทนที่เลือก" };
            }

            if (!substituteCoach.IsActive)
            {
                return new SubstituteCoachActionResult { Error = "ไม่สามารถกำหนดโค้ชที่ปิดใช้งานเป็นโค้ชตัวแทนได้" };
            }

            var conflicts = await _conflictService.CheckCoachOverlapAsync(
                substituteCoach.CoachId,
                session.ScheduledStartDateTime,
                session.ScheduledEndDateTime,
                excludeTrainingSessionId: session.TrainingSessionId);

            // FR-CONFLICT-004: an authorized Administrator (this controller is
            // Administrator-only) may proceed past a detected conflict only by
            // supplying an override reason; otherwise the conflict still blocks.
            var overrideReason = conflicts.Count > 0 && request.OverrideConflict ? request.OverrideReason?.Trim() : null;
            if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
            {
                return new SubstituteCoachActionResult
                {
                    Error = "โค้ชตัวแทนมีตารางฝึกซ้อมทับซ้อน",
                    Conflicts = conflicts,
                };
            }

            var actionDate = DateTime.UtcNow;
            var history = new CoachSubstitutionHistory
            {
                TrainingSessionId = session.TrainingSessionId,
                OriginalCoachId = session.AssignedCoachId,
                SubstituteCoachId = substituteCoach.CoachId,
                Reason = reason,
                ActionByUserId = actionByUserId,
                ActionDate = actionDate,
            };

            // FR-SUB-003/004: the scheduled assignment and its identity snapshots
            // never change. Only the separate actual-coach fields are updated.
            session.ActualCoachId = substituteCoach.CoachId;
            session.ActualCoachCodeSnapshot = substituteCoach.CoachCode;
            session.ActualCoachNameSnapshot = substituteCoach.FullName;
            if (overrideReason is not null)
            {
                session.IsConflictOverridden = true;
                session.ConflictOverrideReason = overrideReason;
            }

            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = actionDate;

            _db.CoachSubstitutionHistories.Add(history);

            // FR-CONFLICT-005 — the override itself remains identifiable in history.
            foreach (var conflict in conflicts)
            {
                _db.ConflictOverrideHistories.Add(new ConflictOverrideHistory
                {
                    ConflictType = Enum.Parse<ConflictType>(conflict.ConflictType),
                    TrainingSessionId = session.TrainingSessionId,
                    Reason = overrideReason!,
                    ActionByUserId = actionByUserId,
                    ActionDate = actionDate,
                });
            }

            await _db.SaveChangesAsync();

            return new SubstituteCoachActionResult
            {
                Data = new SubstituteCoachResponseDto
                {
                    Session = TrainingSessionMapper.ToDetailDto(session),
                    Substitution = new CoachSubstitutionHistoryDto
                    {
                        CoachSubstitutionHistoryId = history.CoachSubstitutionHistoryId,
                        TrainingSessionId = session.TrainingSessionId,
                        OriginalCoachId = session.AssignedCoachId,
                        OriginalCoachCode = session.AssignedCoachCodeSnapshot,
                        OriginalCoachName = session.AssignedCoachNameSnapshot,
                        SubstituteCoachId = substituteCoach.CoachId,
                        SubstituteCoachCode = substituteCoach.CoachCode,
                        SubstituteCoachName = substituteCoach.FullName,
                        Reason = history.Reason,
                        ActionByUserId = history.ActionByUserId,
                        ActionDate = history.ActionDate,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to assign substitute coach. Controller: SubstituteCoachesController Service: SubstituteCoachService Function: AssignAsync TrainingSessionId: {TrainingSessionId} SubstituteCoachId: {SubstituteCoachId} ActionByUserId: {ActionByUserId}",
                trainingSessionId,
                request.SubstituteCoachId,
                actionByUserId);
            throw;
        }
    }
}
