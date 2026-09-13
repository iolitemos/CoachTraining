using CoachTraining.Api.DTOs.PublicCalendar;

namespace CoachTraining.Api.Services;

public interface IPublicRoutineCalendarService
{
    Task<RoutineCalendarShareStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<RoutineCalendarShareCreatedDto> RotateLinkAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> RevokeLinkAsync(int userId, CancellationToken cancellationToken = default);
    Task<PublicRoutineCalendarDto?> GetCalendarAsync(
        string token,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
