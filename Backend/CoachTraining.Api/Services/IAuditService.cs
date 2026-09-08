using CoachTraining.Api.DTOs.Approvals;
using CoachTraining.Api.DTOs.History;
using CoachTraining.Api.DTOs.Substitutions;

namespace CoachTraining.Api.Services;

/// <summary>
/// Session business history / audit trail (requirement.md 6.20, FR-AUDIT-001–003,
/// todo.md 4.20). Every signed-in role may call this; a Coach account only ever
/// sees history for its own sessions (CLAUDE.md section 11), enforced here.
/// </summary>
public interface IAuditService
{
    /// <summary>General audit trail (schedule/cancellation/reschedule events, etc.) for one session.</summary>
    Task<HistoryAccessResult<List<AuditLogEntryDto>>> GetSessionAuditLogAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    Task<HistoryAccessResult<List<TrainingApprovalHistoryDto>>> GetApprovalHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    Task<HistoryAccessResult<List<CoachSubstitutionHistoryDto>>> GetSubstitutionHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);

    Task<HistoryAccessResult<List<ConflictOverrideHistoryEntryDto>>> GetConflictOverrideHistoryAsync(int trainingSessionId, bool isPrivilegedRole, int? currentCoachId);
}
