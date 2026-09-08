using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Approvals;
using CoachTraining.Api.DTOs.History;
using CoachTraining.Api.DTOs.Substitutions;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Session business history / audit trail (requirement.md 6.20, todo.md 4.20).</summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HistoryAccessResult<List<AuditLogEntryDto>>> GetSessionAuditLogAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var access = await CheckAccessAsync<List<AuditLogEntryDto>>(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (access is not null)
        {
            return access;
        }

        var entries = await _db.AuditLogs
            .Where(a => a.EntityName == "TrainingSession" && a.EntityId == trainingSessionId)
            .OrderByDescending(a => a.ActionDate)
            .Select(a => new AuditLogEntryDto
            {
                AuditLogId = a.AuditLogId,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                PreviousValue = a.PreviousValue,
                NewValue = a.NewValue,
                ActionByUserId = a.ActionByUserId,
                ActionDate = a.ActionDate,
            })
            .ToListAsync();

        return new HistoryAccessResult<List<AuditLogEntryDto>> { Data = entries };
    }

    public async Task<HistoryAccessResult<List<TrainingApprovalHistoryDto>>> GetApprovalHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var access = await CheckAccessAsync<List<TrainingApprovalHistoryDto>>(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (access is not null)
        {
            return access;
        }

        var entries = await _db.TrainingApprovalHistories
            .Where(h => h.TrainingSessionId == trainingSessionId)
            .OrderByDescending(h => h.ActionDate)
            .Select(h => new TrainingApprovalHistoryDto
            {
                TrainingApprovalHistoryId = h.TrainingApprovalHistoryId,
                TrainingSessionId = h.TrainingSessionId,
                ActionType = h.ActionType,
                Reason = h.Reason,
                ActionByUserId = h.ActionByUserId,
                ActionDate = h.ActionDate,
            })
            .ToListAsync();

        return new HistoryAccessResult<List<TrainingApprovalHistoryDto>> { Data = entries };
    }

    public async Task<HistoryAccessResult<List<CoachSubstitutionHistoryDto>>> GetSubstitutionHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var access = await CheckAccessAsync<List<CoachSubstitutionHistoryDto>>(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (access is not null)
        {
            return access;
        }

        var entries = await _db.CoachSubstitutionHistories
            .Include(h => h.OriginalCoach)
            .Include(h => h.SubstituteCoach)
            .Where(h => h.TrainingSessionId == trainingSessionId)
            .OrderByDescending(h => h.ActionDate)
            .Select(h => new CoachSubstitutionHistoryDto
            {
                CoachSubstitutionHistoryId = h.CoachSubstitutionHistoryId,
                TrainingSessionId = h.TrainingSessionId,
                OriginalCoachId = h.OriginalCoachId,
                OriginalCoachCode = h.OriginalCoach.CoachCode,
                OriginalCoachName = h.OriginalCoach.FullName,
                SubstituteCoachId = h.SubstituteCoachId,
                SubstituteCoachCode = h.SubstituteCoach.CoachCode,
                SubstituteCoachName = h.SubstituteCoach.FullName,
                Reason = h.Reason,
                ActionByUserId = h.ActionByUserId,
                ActionDate = h.ActionDate,
            })
            .ToListAsync();

        return new HistoryAccessResult<List<CoachSubstitutionHistoryDto>> { Data = entries };
    }

    public async Task<HistoryAccessResult<List<ConflictOverrideHistoryEntryDto>>> GetConflictOverrideHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var access = await CheckAccessAsync<List<ConflictOverrideHistoryEntryDto>>(trainingSessionId, isPrivilegedRole, currentCoachId);
        if (access is not null)
        {
            return access;
        }

        var entries = await _db.ConflictOverrideHistories
            .Where(h => h.TrainingSessionId == trainingSessionId)
            .OrderByDescending(h => h.ActionDate)
            .Select(h => new ConflictOverrideHistoryEntryDto
            {
                ConflictOverrideHistoryId = h.ConflictOverrideHistoryId,
                ConflictType = h.ConflictType,
                TrainingSessionId = h.TrainingSessionId,
                RoutineScheduleId = h.RoutineScheduleId,
                Reason = h.Reason,
                ActionByUserId = h.ActionByUserId,
                ActionDate = h.ActionDate,
            })
            .ToListAsync();

        return new HistoryAccessResult<List<ConflictOverrideHistoryEntryDto>> { Data = entries };
    }

    /// <summary>Shared not-found/forbidden check (CLAUDE.md section 11 — a Coach never
    /// sees another coach's operational records). Returns null when access is granted.</summary>
    private async Task<HistoryAccessResult<T>?> CheckAccessAsync<T>(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId)
    {
        var session = await _db.TrainingSessions
            .Where(s => s.TrainingSessionId == trainingSessionId)
            .Select(s => new { s.AssignedCoachId, s.ActualCoachId })
            .FirstOrDefaultAsync();

        if (session is null)
        {
            return new HistoryAccessResult<T> { NotFound = true };
        }

        if (!isPrivilegedRole && session.AssignedCoachId != currentCoachId && session.ActualCoachId != currentCoachId)
        {
            return new HistoryAccessResult<T> { Forbidden = true };
        }

        return null;
    }
}
