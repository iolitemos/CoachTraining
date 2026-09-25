using CoachTraining.Api.DTOs.ParentRoutinePlans;

namespace CoachTraining.Api.Services;

public interface IRoutineTrainingDateService
{
    Task<IReadOnlyList<RoutineTrainingDateDto>> ListAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<RoutineTrainingDateDto> AddAsync(DateOnly trainingDate, int userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(DateOnly trainingDate, int userId, CancellationToken cancellationToken = default);
}
