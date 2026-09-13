using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.CalendarNotes;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class CalendarNoteService : ICalendarNoteService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CalendarNoteService> _logger;

    public CalendarNoteService(ApplicationDbContext db, ILogger<CalendarNoteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CalendarNoteDto>> ListAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _db.CalendarNotes.AsNoTracking()
            .Where(note => note.NoteDate >= startDate && note.NoteDate <= endDate)
            .OrderBy(note => note.NoteDate)
            .Select(note => new CalendarNoteDto(note.CalendarNoteId, note.NoteDate, note.Content))
            .ToListAsync(cancellationToken);
    }

    public async Task<CalendarNoteDto> UpsertAsync(DateOnly noteDate, string content, int actionByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var trimmedContent = content.Trim();
            var note = await _db.CalendarNotes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(item => item.NoteDate == noteDate, cancellationToken);
            if (note is null)
            {
                note = new CalendarNote { NoteDate = noteDate, Content = trimmedContent, CreatedByUserId = actionByUserId };
                _db.CalendarNotes.Add(note);
            }
            else
            {
                note.Content = trimmedContent;
                note.IsDeleted = false;
                note.UpdatedDate = DateTime.UtcNow;
                note.UpdatedByUserId = actionByUserId;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CalendarNoteDto(note.CalendarNoteId, note.NoteDate, note.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: CalendarNoteService Function: UpsertAsync NoteDate: {NoteDate} UserId: {UserId}", noteDate, actionByUserId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(DateOnly noteDate, int actionByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var note = await _db.CalendarNotes.FirstOrDefaultAsync(item => item.NoteDate == noteDate, cancellationToken);
            if (note is null) return false;
            note.IsDeleted = true;
            note.UpdatedDate = DateTime.UtcNow;
            note.UpdatedByUserId = actionByUserId;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: CalendarNoteService Function: DeleteAsync NoteDate: {NoteDate} UserId: {UserId}", noteDate, actionByUserId);
            throw;
        }
    }
}
