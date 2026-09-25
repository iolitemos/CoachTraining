using CoachTraining.Api.DTOs.ParentRoutinePlans;

namespace CoachTraining.Api.Services;

public interface IParentRoutinePlanService
{
    Task<ParentRoutinePlanLinkStatusDto?> GetLinkStatusAsync(int athleteId, CancellationToken cancellationToken = default);
    Task<ParentRoutinePlanLinkCreatedDto?> RotateLinkAsync(int athleteId, int userId, CancellationToken cancellationToken = default);
    Task<ParentRoutinePlanLinkStatusDto?> SetLinkAccessAsync(int athleteId, bool isEnabled, int userId, CancellationToken cancellationToken = default);
    Task<ParentRoutinePlanCalendarDto?> GetCalendarAsync(string token, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<ParentRoutinePlanCalendarDto?> SaveAsync(string token, DateOnly startDate, DateOnly endDate, IReadOnlyCollection<DateOnly> selectedDates, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutineParticipationPlanSummaryDto>> GetSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoutineParticipationPlanAthleteDto>?> GetAthletesAsync(DateOnly trainingDate, CancellationToken cancellationToken = default);
}
