using System.Text.Json;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.Reschedules;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Rescheduling for eligible sessions (requirement.md 6.13, FR-CR-004–007, todo.md 4.13).</summary>
public class ReschedulingService : IReschedulingService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly IScheduleConflictService _conflictService;
    private readonly ILogger<ReschedulingService> _logger;

    public ReschedulingService(
        ApplicationDbContext db,
        ISessionStatusService sessionStatusService,
        IScheduleConflictService conflictService,
        ILogger<ReschedulingService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _conflictService = conflictService;
        _logger = logger;
    }

    public async Task<RescheduleActionResult> RescheduleAsync(int trainingSessionId, RescheduleSessionRequest request, int actionByUserId)
    {
        try
        {
            var original = await _db.TrainingSessions
                .Include(s => s.PrivateAthletes)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (original is null)
            {
                return new RescheduleActionResult { NotFound = true };
            }

            // The centralized status graph permits rescheduling only from
            // Scheduled or CoachAbsent — the same eligibility as Cancellation's
            // "future or uncompleted session" (FR-CR-004), reused here rather
            // than re-declared.
            if (!_sessionStatusService.CanTransition(original.Status, SessionStatus.Rescheduled))
            {
                return new RescheduleActionResult { Error = $"ไม่สามารถเลื่อนเซสชันในสถานะปัจจุบัน ({original.Status}) ได้" };
            }

            var newStart = request.SessionDate.ToDateTime(request.StartTime);
            var newEnd = request.SessionDate.ToDateTime(request.EndTime);

            var conflicts = await CheckConflictsAsync(original, newStart, newEnd);

            // FR-CONFLICT-004: an authorized Administrator (this controller is
            // Administrator-only) may proceed past a detected conflict only by
            // supplying an override reason; otherwise the conflict still blocks.
            var overrideReason = conflicts.Count > 0 && request.OverrideConflict ? request.OverrideReason?.Trim() : null;
            if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
            {
                return new RescheduleActionResult { Error = "พบตารางฝึกซ้อมทับซ้อนกับเวลาที่เลื่อนไป", Conflicts = conflicts };
            }

            var actionDate = DateTime.UtcNow;
            var previousStatus = original.Status;

            var replacement = new TrainingSession
            {
                TrainingType = original.TrainingType,
                RoutineScheduleId = original.RoutineScheduleId,
                SessionDate = request.SessionDate,
                ScheduledStartDateTime = newStart,
                ScheduledEndDateTime = newEnd,
                AssignedCoachId = original.AssignedCoachId,
                AssignedCoachCodeSnapshot = original.AssignedCoachCodeSnapshot,
                AssignedCoachNameSnapshot = original.AssignedCoachNameSnapshot,
                Location = original.Location,
                Remarks = request.Remarks ?? original.Remarks,
                Status = SessionStatus.Scheduled,
                OriginalSessionId = original.TrainingSessionId,
                IsConflictOverridden = overrideReason is not null,
                ConflictOverrideReason = overrideReason,
                CreatedByUserId = actionByUserId,
            };

            // FR-PRIVATE-003/004 — the assigned athlete roster carries over to the
            // replacement session; attendance is recorded fresh against it.
            foreach (var assignment in original.PrivateAthletes)
            {
                replacement.PrivateAthletes.Add(new PrivateSessionAthlete
                {
                    AthleteId = assignment.AthleteId,
                    IsGuest = assignment.IsGuest,
                    AthleteCodeSnapshot = assignment.AthleteCodeSnapshot,
                    AthleteNameSnapshot = assignment.AthleteNameSnapshot,
                    GuestPhone = assignment.GuestPhone,
                    GuestRemark = assignment.GuestRemark,
                    CreatedByUserId = actionByUserId,
                });
            }

            // FR-SESSION-010/FR-CR-006: the original is preserved, not deleted —
            // it becomes Rescheduled and is excluded from completed-teaching and
            // attendance totals by status alone (FR-CR-007, FR-STATUS-004).
            original.Status = SessionStatus.Rescheduled;
            original.UpdatedByUserId = actionByUserId;
            original.UpdatedDate = actionDate;

            _db.TrainingSessions.Add(replacement);

            _db.AuditLogs.Add(new AuditLog
            {
                EntityName = nameof(TrainingSession),
                EntityId = original.TrainingSessionId,
                Action = "Reschedule",
                PreviousValue = JsonSerializer.Serialize(new
                {
                    Status = previousStatus.ToString(),
                    original.ScheduledStartDateTime,
                    original.ScheduledEndDateTime,
                }),
                NewValue = JsonSerializer.Serialize(new
                {
                    Status = SessionStatus.Rescheduled.ToString(),
                    ReplacementScheduledStartDateTime = newStart,
                    ReplacementScheduledEndDateTime = newEnd,
                }),
                ActionByUserId = actionByUserId,
                ActionDate = actionDate,
            });

            await _db.SaveChangesAsync();

            // FR-CONFLICT-005 — the override itself remains identifiable in history.
            await RecordConflictOverrideAsync(conflicts, overrideReason, replacement.TrainingSessionId, actionByUserId, actionDate);

            return new RescheduleActionResult
            {
                OriginalSession = TrainingSessionMapper.ToDetailDto(original),
                ReplacementSession = TrainingSessionMapper.ToDetailDto(replacement),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to reschedule training session. Controller: ReschedulingController Service: ReschedulingService Function: RescheduleAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}",
                trainingSessionId,
                actionByUserId);
            throw;
        }
    }

    private async Task<List<ConflictDetail>> CheckConflictsAsync(TrainingSession original, DateTime newStart, DateTime newEnd)
    {
        var conflicts = new List<ConflictDetail>();
        conflicts.AddRange(await _conflictService.CheckCoachOverlapAsync(
            original.AssignedCoachId, newStart, newEnd, excludeTrainingSessionId: original.TrainingSessionId));

        if (original.TrainingType == TrainingType.Private && original.PrivateAthletes.Count > 0)
        {
            var athleteIds = original.PrivateAthletes.Where(psa => psa.AthleteId.HasValue).Select(psa => psa.AthleteId!.Value).ToList();
            conflicts.AddRange(await _conflictService.CheckAthleteOverlapAsync(
                athleteIds, newStart, newEnd, excludeTrainingSessionId: original.TrainingSessionId));
        }

        return conflicts;
    }

    /// <summary>FR-CONFLICT-004/005 — records one history row per detected conflict once an
    /// authorized Administrator has supplied an override reason. No-op when there is
    /// nothing to override.</summary>
    private async Task RecordConflictOverrideAsync(
        List<ConflictDetail> conflicts, string? overrideReason, int replacementTrainingSessionId, int actionByUserId, DateTime actionDate)
    {
        if (conflicts.Count == 0 || string.IsNullOrWhiteSpace(overrideReason))
        {
            return;
        }

        foreach (var conflict in conflicts)
        {
            _db.ConflictOverrideHistories.Add(new ConflictOverrideHistory
            {
                ConflictType = Enum.Parse<ConflictType>(conflict.ConflictType),
                TrainingSessionId = replacementTrainingSessionId,
                Reason = overrideReason,
                ActionByUserId = actionByUserId,
                ActionDate = actionDate,
            });
        }

        await _db.SaveChangesAsync();
    }
}
