using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.TrainingSessions;
using CoachTraining.Api.Helpers;
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
                TrainingType = s.TrainingType,
                SessionDate = s.SessionDate,
                ScheduledStartDateTime = s.ScheduledStartDateTime,
                ScheduledEndDateTime = s.ScheduledEndDateTime,
                ActualStartDateTime = s.ActualStartDateTime,
                ActualEndDateTime = s.ActualEndDateTime,
                AssignedCoachCode = s.AssignedCoachCodeSnapshot,
                AssignedCoachName = s.AssignedCoachNameSnapshot,
                ActualCoachCode = s.ActualCoachCodeSnapshot,
                ActualCoachName = s.ActualCoachNameSnapshot,
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
}
