using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.ParentRoutinePlans;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class RoutineTrainingDateService : IRoutineTrainingDateService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RoutineTrainingDateService> _logger;

    public RoutineTrainingDateService(ApplicationDbContext db, ILogger<RoutineTrainingDateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RoutineTrainingDateDto>> ListAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
        await _db.RoutineTrainingDates.AsNoTracking()
            .Where(item => item.TrainingDate >= startDate && item.TrainingDate <= endDate)
            .OrderBy(item => item.TrainingDate)
            .Select(item => new RoutineTrainingDateDto(item.RoutineTrainingDateId, item.TrainingDate))
            .ToListAsync(cancellationToken);

    public async Task<RoutineTrainingDateDto> AddAsync(DateOnly trainingDate, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _db.RoutineTrainingDates.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.TrainingDate == trainingDate, cancellationToken);
            if (existing is not null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.UpdatedDate = DateTime.UtcNow;
                    existing.UpdatedByUserId = userId;
                    await _db.SaveChangesAsync(cancellationToken);
                }
                return new(existing.RoutineTrainingDateId, existing.TrainingDate);
            }

            var item = new RoutineTrainingDate { TrainingDate = trainingDate, CreatedByUserId = userId };
            _db.RoutineTrainingDates.Add(item);
            await _db.SaveChangesAsync(cancellationToken);
            return new(item.RoutineTrainingDateId, item.TrainingDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: RoutineTrainingDateService Function: AddAsync UserId: {UserId} TrainingDate: {TrainingDate}", userId, trainingDate);
            throw;
        }
    }

    public async Task<bool> RemoveAsync(DateOnly trainingDate, int userId, CancellationToken cancellationToken = default)
    {
        var item = await _db.RoutineTrainingDates.SingleOrDefaultAsync(date => date.TrainingDate == trainingDate, cancellationToken);
        if (item is null) return false;
        item.IsDeleted = true;
        item.UpdatedDate = DateTime.UtcNow;
        item.UpdatedByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
