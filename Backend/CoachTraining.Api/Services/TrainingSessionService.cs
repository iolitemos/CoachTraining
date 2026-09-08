using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class TrainingSessionService : ITrainingSessionService
{
    private readonly ApplicationDbContext _db;

    public TrainingSessionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<TrainingSessionListItemDto>> ListAsync(TrainingSessionFilterRequest filter, bool isPrivilegedRole, int? currentCoachId)
    {
        if (!isPrivilegedRole && currentCoachId is null)
        {
            // Coach account not yet linked to a Coach record — nothing to scope to.
            return new PagedResponse<TrainingSessionListItemDto>([], filter.Page, filter.PageSize, 0);
        }

        var query = _db.TrainingSessions.AsQueryable();

        if (!isPrivilegedRole)
        {
            query = query.Where(s => s.AssignedCoachId == currentCoachId || s.ActualCoachId == currentCoachId);
        }
        else if (filter.CoachId is not null)
        {
            query = query.Where(s => s.AssignedCoachId == filter.CoachId || s.ActualCoachId == filter.CoachId);
        }

        if (filter.TrainingType is not null)
        {
            query = query.Where(s => s.TrainingType == filter.TrainingType);
        }

        if (filter.Status is not null)
        {
            query = query.Where(s => s.Status == filter.Status);
        }

        if (filter.DateFrom is not null)
        {
            query = query.Where(s => s.SessionDate >= filter.DateFrom.Value);
        }

        if (filter.DateTo is not null)
        {
            query = query.Where(s => s.SessionDate <= filter.DateTo.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.SessionDate)
            .ThenByDescending(s => s.ScheduledStartDateTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new TrainingSessionListItemDto
            {
                TrainingSessionId = s.TrainingSessionId,
                RoutineScheduleId = s.RoutineScheduleId,
                TrainingType = s.TrainingType,
                SessionDate = s.SessionDate,
                ScheduledStartDateTime = s.ScheduledStartDateTime,
                ScheduledEndDateTime = s.ScheduledEndDateTime,
                ActualStartDateTime = s.ActualStartDateTime,
                ActualEndDateTime = s.ActualEndDateTime,
                AssignedCoachCode = s.AssignedCoachCodeSnapshot,
                AssignedCoachName = s.AssignedCoachNameSnapshot,
                AssignedCoachNickname = s.AssignedCoach.Nickname,
                AssignedCoachColorHex = s.AssignedCoach.ColorHex,
                ActualCoachCode = s.ActualCoachCodeSnapshot,
                ActualCoachName = s.ActualCoachNameSnapshot,
                ActualCoachNickname = s.ActualCoach != null ? s.ActualCoach.Nickname : null,
                ActualCoachColorHex = s.ActualCoach != null ? s.ActualCoach.ColorHex : null,
                Status = s.Status,
                Location = s.Location,
            })
            .ToListAsync();

        return new PagedResponse<TrainingSessionListItemDto>(items, filter.Page, filter.PageSize, totalCount);
    }

    public async Task<TrainingSessionDetailDto?> GetByIdAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.PrivateAthletes)
            .Include(s => s.TrainingLog)
            .Include(s => s.AssignedCoach)
            .Include(s => s.ActualCoach)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);

        if (session is null)
        {
            return null;
        }

        var belongsToCurrentCoach = session.AssignedCoachId == currentCoachId || session.ActualCoachId == currentCoachId;
        if (!isPrivilegedRole && !belongsToCurrentCoach)
        {
            return null;
        }

        return TrainingSessionMapper.ToDetailDto(session);
    }

    public async Task<(TrainingSessionDetailDto? Session, string? Error, bool NotFound)> ResetToScheduledAsync(int trainingSessionId, string reason, int actionByUserId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.PrivateAthletes)
            .Include(s => s.TrainingLog)
            .Include(s => s.AssignedCoach)
            .Include(s => s.ActualCoach)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId);
        if (session is null) return (null, null, true);

        var trimmedReason = reason.Trim();
        if (string.IsNullOrWhiteSpace(trimmedReason)) return (null, "กรุณาระบุเหตุผลในการดึงสถานะกลับ", false);
        if (session.Status != SessionStatus.InProgress) return (null, "ดึงกลับเป็นกำหนดการได้เฉพาะเซสชันที่กำลังฝึกซ้อม", false);

        var actionDate = DateTime.UtcNow;
        var previousStart = session.ActualStartDateTime;
        var previousActualCoachId = session.ActualCoachId;
        var previousActualCoachCode = session.ActualCoachCodeSnapshot;
        var previousActualCoachName = session.ActualCoachNameSnapshot;
        session.Status = SessionStatus.Scheduled;
        session.ActualStartDateTime = null;
        session.ActualEndDateTime = null;
        session.ActualCoachId = null;
        session.ActualCoach = null;
        session.ActualCoachCodeSnapshot = null;
        session.ActualCoachNameSnapshot = null;
        session.UpdatedByUserId = actionByUserId;
        session.UpdatedDate = actionDate;
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(TrainingSession), EntityId = trainingSessionId, Action = "ResetToScheduled",
            PreviousValue = JsonSerializer.Serialize(new
            {
                Status = SessionStatus.InProgress.ToString(),
                ActualStartDateTime = previousStart,
                ActualCoachId = previousActualCoachId,
                ActualCoachCode = previousActualCoachCode,
                ActualCoachName = previousActualCoachName,
            }),
            NewValue = JsonSerializer.Serialize(new { Status = SessionStatus.Scheduled.ToString(), Reason = trimmedReason }),
            ActionByUserId = actionByUserId, ActionDate = actionDate,
        });
        await _db.SaveChangesAsync();
        return (TrainingSessionMapper.ToDetailDto(session), null, false);
    }
}
