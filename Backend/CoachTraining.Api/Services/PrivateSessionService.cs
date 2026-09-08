using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Conflicts;
using CoachTraining.Api.DTOs.PrivateSessions;
using CoachTraining.Api.Models;
using CoachTraining.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Private Training Management (requirement.md 4.5, todo.md 4.4).</summary>
public class PrivateSessionService : IPrivateSessionService
{
    private readonly ApplicationDbContext _db;
    private readonly IScheduleConflictService _conflictService;
    private readonly ILogger<PrivateSessionService> _logger;

    public PrivateSessionService(ApplicationDbContext db, IScheduleConflictService conflictService, ILogger<PrivateSessionService> logger)
    {
        _db = db;
        _conflictService = conflictService;
        _logger = logger;
    }

    public async Task<PagedResponse<PrivateSessionListItemDto>> ListAsync(PagedRequest request)
    {
        var query = _db.TrainingSessions
            .Where(s => s.TrainingType == TrainingType.Private)
            .Include(s => s.AssignedCoach)
            .Include(s => s.PrivateAthletes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(s =>
                s.AssignedCoachCodeSnapshot.ToUpper().Contains(search) ||
                s.AssignedCoachNameSnapshot.ToUpper().Contains(search) ||
                (s.Location != null && s.Location.ToUpper().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.SessionDate)
            .ThenBy(s => s.ScheduledStartDateTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new PrivateSessionListItemDto
            {
                TrainingSessionId = s.TrainingSessionId,
                SessionDate = s.SessionDate,
                StartTime = TimeOnly.FromDateTime(s.ScheduledStartDateTime),
                EndTime = TimeOnly.FromDateTime(s.ScheduledEndDateTime),
                CoachCode = s.AssignedCoachCodeSnapshot,
                CoachFullName = s.AssignedCoachNameSnapshot,
                Location = s.Location,
                Status = s.Status,
                AthleteCount = s.PrivateAthletes.Count,
            })
            .ToListAsync();

        return new PagedResponse<PrivateSessionListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<PrivateSessionDetailDto?> GetByIdAsync(int trainingSessionId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.PrivateAthletes)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId && s.TrainingType == TrainingType.Private);

        return session is null ? null : MapToDetail(session);
    }

    public async Task<PrivateSessionSaveResult> CreateAsync(PrivateSessionCreateDto dto, int actionByUserId)
    {
        try
        {
            // FR-PRIVATE-003: enforced here too, not only via the DTO's [MinLength],
            // since business validation must not rely solely on the API boundary.
            if (dto.AthleteIds.Count == 0)
            {
                return new PrivateSessionSaveResult { Error = "กรุณาเลือกนักกีฬาอย่างน้อยหนึ่งคน" };
            }

            var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == dto.CoachId);
            if (coach is null)
            {
                return new PrivateSessionSaveResult { Error = "ไม่พบข้อมูลโค้ชที่เลือก" };
            }

            if (!coach.IsActive)
            {
                return new PrivateSessionSaveResult { Error = "ไม่สามารถกำหนดเซสชันให้โค้ชที่ปิดใช้งานได้" };
            }

            var (athletes, athleteError) = await LoadAndValidateAthletesAsync(dto.AthleteIds);
            if (athleteError is not null)
            {
                return new PrivateSessionSaveResult { Error = athleteError };
            }

            var scheduledStart = dto.SessionDate.ToDateTime(dto.StartTime);
            var scheduledEnd = dto.SessionDate.ToDateTime(dto.EndTime);

            var conflicts = await CheckConflictsAsync(dto.CoachId, dto.AthleteIds, scheduledStart, scheduledEnd);

            // FR-CONFLICT-004: an authorized Administrator (this controller is
            // Administrator-only) may proceed past a detected conflict only by
            // supplying an override reason; otherwise the conflict still blocks.
            var overrideReason = conflicts.Count > 0 && dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
            if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
            {
                return new PrivateSessionSaveResult { Error = "พบตารางฝึกซ้อมทับซ้อน", Conflicts = conflicts };
            }

            var session = new TrainingSession
            {
                TrainingType = TrainingType.Private,
                SessionDate = dto.SessionDate,
                ScheduledStartDateTime = scheduledStart,
                ScheduledEndDateTime = scheduledEnd,
                AssignedCoachId = coach.CoachId,
                AssignedCoachCodeSnapshot = coach.CoachCode,
                AssignedCoachNameSnapshot = coach.FullName,
                Location = dto.Location,
                Remarks = dto.Remarks,
                Status = SessionStatus.Scheduled,
                IsConflictOverridden = overrideReason is not null,
                ConflictOverrideReason = overrideReason,
                CreatedByUserId = actionByUserId,
            };

            foreach (var athlete in athletes)
            {
                session.PrivateAthletes.Add(new PrivateSessionAthlete
                {
                    AthleteId = athlete.AthleteId,
                    AthleteCodeSnapshot = athlete.AthleteCode,
                    AthleteNameSnapshot = athlete.FullName,
                    CreatedByUserId = actionByUserId,
                });
            }

            // Session + its athlete assignments are added in one SaveChangesAsync
            // call, so EF Core's implicit transaction keeps them atomic.
            _db.TrainingSessions.Add(session);
            await _db.SaveChangesAsync();

            // FR-CONFLICT-005 — the override itself remains identifiable in history.
            await RecordConflictOverrideAsync(conflicts, overrideReason, session.TrainingSessionId, actionByUserId);

            return new PrivateSessionSaveResult { Session = MapToDetail(session) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create private session. Controller: PrivateSessionsController Service: PrivateSessionService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<PrivateSessionSaveResult> UpdateAsync(int trainingSessionId, PrivateSessionUpdateDto dto, int actionByUserId)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.PrivateAthletes)
            .FirstOrDefaultAsync(s => s.TrainingSessionId == trainingSessionId && s.TrainingType == TrainingType.Private);

        if (session is null)
        {
            return new PrivateSessionSaveResult();
        }

        if (session.Status != SessionStatus.Scheduled)
        {
            return new PrivateSessionSaveResult { Error = "ไม่สามารถแก้ไขได้ เนื่องจากเซสชันเริ่มดำเนินการแล้วหรือถูกล็อก" };
        }

        if (dto.AthleteIds.Count == 0)
        {
            return new PrivateSessionSaveResult { Error = "กรุณาเลือกนักกีฬาอย่างน้อยหนึ่งคน" };
        }

        var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == dto.CoachId);
        if (coach is null)
        {
            return new PrivateSessionSaveResult { Error = "ไม่พบข้อมูลโค้ชที่เลือก" };
        }

        if (!coach.IsActive)
        {
            return new PrivateSessionSaveResult { Error = "ไม่สามารถกำหนดเซสชันให้โค้ชที่ปิดใช้งานได้" };
        }

        var (athletes, athleteError) = await LoadAndValidateAthletesAsync(dto.AthleteIds);
        if (athleteError is not null)
        {
            return new PrivateSessionSaveResult { Error = athleteError };
        }

        var scheduledStart = dto.SessionDate.ToDateTime(dto.StartTime);
        var scheduledEnd = dto.SessionDate.ToDateTime(dto.EndTime);

        var conflicts = await CheckConflictsAsync(dto.CoachId, dto.AthleteIds, scheduledStart, scheduledEnd, excludeTrainingSessionId: trainingSessionId);
        var overrideReason = conflicts.Count > 0 && dto.OverrideConflict ? dto.OverrideReason?.Trim() : null;
        if (conflicts.Count > 0 && string.IsNullOrWhiteSpace(overrideReason))
        {
            return new PrivateSessionSaveResult { Error = "พบตารางฝึกซ้อมทับซ้อน", Conflicts = conflicts };
        }

        session.AssignedCoachId = coach.CoachId;
        session.AssignedCoachCodeSnapshot = coach.CoachCode;
        session.AssignedCoachNameSnapshot = coach.FullName;
        session.SessionDate = dto.SessionDate;
        session.ScheduledStartDateTime = scheduledStart;
        session.ScheduledEndDateTime = scheduledEnd;
        session.Location = dto.Location;
        session.Remarks = dto.Remarks;
        // Recomputed fresh on every save: a conflict-free update clears any
        // previously recorded override so the flag never outlives its cause.
        session.IsConflictOverridden = overrideReason is not null;
        session.ConflictOverrideReason = overrideReason;
        session.UpdatedByUserId = actionByUserId;
        session.UpdatedDate = DateTime.UtcNow;

        _db.PrivateSessionAthletes.RemoveRange(session.PrivateAthletes);
        session.PrivateAthletes.Clear();
        foreach (var athlete in athletes)
        {
            session.PrivateAthletes.Add(new PrivateSessionAthlete
            {
                AthleteId = athlete.AthleteId,
                AthleteCodeSnapshot = athlete.AthleteCode,
                AthleteNameSnapshot = athlete.FullName,
                CreatedByUserId = actionByUserId,
            });
        }

        await _db.SaveChangesAsync();

        // FR-CONFLICT-005 — the override itself remains identifiable in history.
        await RecordConflictOverrideAsync(conflicts, overrideReason, session.TrainingSessionId, actionByUserId);

        return new PrivateSessionSaveResult { Session = MapToDetail(session) };
    }

    private async Task<(List<Athlete> Athletes, string? Error)> LoadAndValidateAthletesAsync(List<int> athleteIds)
    {
        var athletes = await _db.Athletes.Where(a => athleteIds.Contains(a.AthleteId)).ToListAsync();

        if (athletes.Count != athleteIds.Distinct().Count())
        {
            return ([], "ไม่พบข้อมูลนักกีฬาบางรายการที่เลือก");
        }

        if (athletes.Any(a => !a.IsActive))
        {
            return ([], "พบนักกีฬาที่ปิดใช้งานในรายการที่เลือก");
        }

        return (athletes, null);
    }

    private async Task<List<ConflictDetail>> CheckConflictsAsync(
        int coachId, List<int> athleteIds, DateTime scheduledStart, DateTime scheduledEnd, int? excludeTrainingSessionId = null)
    {
        var conflicts = new List<ConflictDetail>();
        conflicts.AddRange(await _conflictService.CheckCoachOverlapAsync(coachId, scheduledStart, scheduledEnd, excludeTrainingSessionId));
        conflicts.AddRange(await _conflictService.CheckAthleteOverlapAsync(athleteIds, scheduledStart, scheduledEnd, excludeTrainingSessionId));
        return conflicts;
    }

    /// <summary>FR-CONFLICT-004/005 — records one history row per detected conflict once an
    /// authorized Administrator has supplied an override reason. No-op when there is
    /// nothing to override.</summary>
    private async Task RecordConflictOverrideAsync(
        List<ConflictDetail> conflicts, string? overrideReason, int trainingSessionId, int actionByUserId)
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
                TrainingSessionId = trainingSessionId,
                Reason = overrideReason,
                ActionByUserId = actionByUserId,
            });
        }

        await _db.SaveChangesAsync();
    }

    private static PrivateSessionDetailDto MapToDetail(TrainingSession session) => new()
    {
        TrainingSessionId = session.TrainingSessionId,
        SessionDate = session.SessionDate,
        StartTime = TimeOnly.FromDateTime(session.ScheduledStartDateTime),
        EndTime = TimeOnly.FromDateTime(session.ScheduledEndDateTime),
        CoachId = session.AssignedCoachId,
        CoachCode = session.AssignedCoachCodeSnapshot,
        CoachFullName = session.AssignedCoachNameSnapshot,
        Location = session.Location,
        Remarks = session.Remarks,
        Status = session.Status,
        IsConflictOverridden = session.IsConflictOverridden,
        ConflictOverrideReason = session.ConflictOverrideReason,
        Athletes = session.PrivateAthletes.Select(psa => new PrivateSessionAthleteDto
        {
            AthleteId = psa.AthleteId,
            AthleteCode = psa.AthleteCodeSnapshot,
            FullName = psa.AthleteNameSnapshot,
        }).ToList(),
    };
}
