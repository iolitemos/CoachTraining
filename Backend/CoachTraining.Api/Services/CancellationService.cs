using System.Text.Json;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Cancellations;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class CancellationService : ICancellationService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<CancellationService> _logger;

    public CancellationService(
        ApplicationDbContext db,
        ISessionStatusService sessionStatusService,
        ILogger<CancellationService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<CancellationActionResult> CancelAsync(
        int trainingSessionId,
        CancelSessionRequest request,
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
                return new CancellationActionResult { NotFound = true };
            }

            var reason = request.Reason.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new CancellationActionResult { Error = "กรุณาระบุเหตุผลในการยกเลิก" };
            }

            // The centralized status graph permits cancellation only from
            // Scheduled, InProgress, or CoachAbsent. Completed/finalized,
            // Cancelled, and rescheduled-original records remain immutable.
            if (!_sessionStatusService.CanTransition(session.Status, SessionStatus.Cancelled))
            {
                return new CancellationActionResult
                {
                    Error = $"ไม่สามารถยกเลิกเซสชันในสถานะปัจจุบัน ({session.Status}) ได้",
                };
            }

            var previousStatus = session.Status;
            var previousReason = session.CancellationReason;
            var actionDate = DateTime.UtcNow;

            session.Status = SessionStatus.Cancelled;
            session.CancellationReason = reason;
            session.UpdatedByUserId = actionByUserId;
            session.UpdatedDate = actionDate;

            _db.AuditLogs.Add(new AuditLog
            {
                EntityName = nameof(TrainingSession),
                EntityId = session.TrainingSessionId,
                Action = "Cancel",
                PreviousValue = JsonSerializer.Serialize(new
                {
                    Status = previousStatus.ToString(),
                    CancellationReason = previousReason,
                }),
                NewValue = JsonSerializer.Serialize(new
                {
                    Status = SessionStatus.Cancelled.ToString(),
                    CancellationReason = reason,
                }),
                ActionByUserId = actionByUserId,
                ActionDate = actionDate,
            });

            await _db.SaveChangesAsync();

            return new CancellationActionResult
            {
                Session = TrainingSessionMapper.ToDetailDto(session),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to cancel training session. Controller: CancellationsController Service: CancellationService Function: CancelAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}",
                trainingSessionId,
                actionByUserId);
            throw;
        }
    }
}
