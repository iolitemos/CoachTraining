using CoachTraining.Api.DTOs.CalendarNotes;

namespace CoachTraining.Api.Services;

public interface ICalendarNoteService
{
    Task<IReadOnlyList<CalendarNoteDto>> ListAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<CalendarNoteDto> UpsertAsync(DateOnly noteDate, string content, int actionByUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(DateOnly noteDate, int actionByUserId, CancellationToken cancellationToken = default);
}
