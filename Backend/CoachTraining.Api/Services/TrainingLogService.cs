using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.TrainingLogs;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class TrainingLogService : ITrainingLogService
{
    private readonly ApplicationDbContext _db;
    private readonly ISessionStatusService _sessionStatusService;
    private readonly ILogger<TrainingLogService> _logger;

    public TrainingLogService(ApplicationDbContext db, ISessionStatusService sessionStatusService, ILogger<TrainingLogService> logger)
    {
        _db = db;
        _sessionStatusService = sessionStatusService;
        _logger = logger;
    }

    public async Task<TrainingLogDto?> GetAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.TrainingLog)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

        if (session is null)
        {
            return null;
        }

        if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
        {
            return null;
        }

        return MapToDto(session.TrainingLog);
    }

    public async Task<TrainingLogActionResult> UpsertAsync(int trainingSessionId, TrainingLogUpsertRequest request, bool isPrivilegedRole, int? currentCoachId, int actionByUserId)
    {
        try
        {
            var session = await _db.TrainingSessions
                .Include(s => s.TrainingLog)
                .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

            if (session is null)
            {
                return new TrainingLogActionResult { NotFound = true };
            }

            if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
            {
                return new TrainingLogActionResult { Forbidden = true };
            }

            // FR-LOG-004 — training log data may be corrected only while the session is editable.
            if (!_sessionStatusService.IsEditableByCoach(session.Status))
            {
                return new TrainingLogActionResult { Error = $"ไม่สามารถแก้ไขบันทึกการฝึกซ้อมได้ในสถานะปัจจุบัน ({session.Status})" };
            }

            var log = session.TrainingLog;
            if (log is null)
            {
                log = new TrainingLog { TrainingSessionId = trainingSessionId, CreatedByUserId = actionByUserId };
                _db.TrainingLogs.Add(log);
            }
            else
            {
                log.UpdatedByUserId = actionByUserId;
                log.UpdatedDate = DateTime.UtcNow;
            }

            log.Topic = request.Topic;
            log.Objective = request.Objective;
            log.ExerciseDrill = request.ExerciseDrill;
            log.Focus = request.Focus;
            log.Intensity = request.Intensity;
            log.CoachNotes = request.CoachNotes;
            log.AthleteNotes = request.AthleteNotes;
            log.GeneralRemarks = request.GeneralRemarks;

            await _db.SaveChangesAsync();

            return new TrainingLogActionResult { TrainingLog = MapToDto(log) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save training log. Controller: TrainingLogController Service: TrainingLogService Function: UpsertAsync TrainingSessionId: {TrainingSessionId} ActionByUserId: {ActionByUserId}", trainingSessionId, actionByUserId);
            throw;
        }
    }

    private static TrainingLogDto MapToDto(TrainingLog? log) => log is null
        ? new TrainingLogDto()
        : new TrainingLogDto
        {
            TrainingLogId = log.TrainingLogId,
            Topic = log.Topic,
            Objective = log.Objective,
            ExerciseDrill = log.ExerciseDrill,
            Focus = log.Focus,
            Intensity = log.Intensity,
            CoachNotes = log.CoachNotes,
            AthleteNotes = log.AthleteNotes,
            GeneralRemarks = log.GeneralRemarks,
            UpdatedDate = log.UpdatedDate,
        };
}
