using System.Security.Cryptography;
using System.Text;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.PublicCalendar;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class PublicRoutineCalendarService : IPublicRoutineCalendarService
{
    private const int MaximumDateRangeDays = 62;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PublicRoutineCalendarService> _logger;

    public PublicRoutineCalendarService(ApplicationDbContext db, ILogger<PublicRoutineCalendarService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RoutineCalendarShareStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var link = await _db.RoutineCalendarShareLinks.AsNoTracking()
            .Where(item => item.RevokedAtUtc == null)
            .OrderByDescending(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
        return new(link is not null, link?.TokenHint, link?.CreatedDate);
    }

    public async Task<RoutineCalendarShareCreatedDto> RotateLinkAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            await RevokeActiveLinksAsync(userId, now, cancellationToken);
            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var link = new RoutineCalendarShareLink
            {
                TokenHash = HashToken(rawToken),
                TokenHint = rawToken[^6..],
                CreatedDate = now,
                CreatedByUserId = userId,
            };
            _db.RoutineCalendarShareLinks.Add(link);
            await _db.SaveChangesAsync(cancellationToken);
            return new(rawToken, link.TokenHint, link.CreatedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: PublicRoutineCalendarService Function: RotateLinkAsync UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RevokeLinkAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var changed = await RevokeActiveLinksAsync(userId, DateTime.UtcNow, cancellationToken);
            if (changed) await _db.SaveChangesAsync(cancellationToken);
            return changed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: PublicRoutineCalendarService Function: RevokeLinkAsync UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<PublicRoutineCalendarDto?> GetCalendarAsync(
        string token, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || endDate < startDate || endDate.DayNumber - startDate.DayNumber > MaximumDateRangeDays)
            return null;

        var tokenHash = HashToken(token);
        var valid = await _db.RoutineCalendarShareLinks.AsNoTracking()
            .AnyAsync(item => item.TokenHash == tokenHash && item.RevokedAtUtc == null, cancellationToken);
        if (!valid) return null;

        var schedules = await _db.RoutineSchedules.AsNoTracking()
            .Where(schedule => schedule.IsActive
                && schedule.EffectiveStartDate >= startDate
                && schedule.EffectiveStartDate <= endDate)
            .OrderBy(schedule => schedule.EffectiveStartDate)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => new PublicRoutineCalendarItemDto(
                schedule.EffectiveStartDate,
                schedule.StartTime,
                schedule.EndTime,
                schedule.Coach.Nickname ?? "โค้ช",
                schedule.Coach.ColorHex))
            .ToListAsync(cancellationToken);

        var competitionMatches = await _db.CompetitionMatches.AsNoTracking()
            .Where(match => match.StartDate <= endDate && match.EndDate >= startDate)
            .OrderBy(match => match.StartDate)
            .ThenBy(match => match.Name)
            .Select(match => new PublicCompetitionMatchDto(
                match.Name,
                match.Province,
                match.StartDate,
                match.EndDate))
            .ToListAsync(cancellationToken);

        var notes = await _db.CalendarNotes.AsNoTracking()
            .Where(note => note.NoteDate >= startDate && note.NoteDate <= endDate)
            .OrderBy(note => note.NoteDate)
            .Select(note => new PublicCalendarNoteDto(note.NoteDate, note.Content))
            .ToListAsync(cancellationToken);

        return new PublicRoutineCalendarDto(schedules, competitionMatches, notes);
    }

    private async Task<bool> RevokeActiveLinksAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var links = await _db.RoutineCalendarShareLinks
            .Where(item => item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var link in links)
        {
            link.RevokedAtUtc = now;
            link.RevokedByUserId = userId;
            link.UpdatedDate = now;
            link.UpdatedByUserId = userId;
        }
        return links.Count > 0;
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
