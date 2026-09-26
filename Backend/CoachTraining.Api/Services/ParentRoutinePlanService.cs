using System.Security.Cryptography;
using System.Text;
using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.ParentRoutinePlans;
using CoachTraining.Api.DTOs.PublicCalendar;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class ParentRoutinePlanService : IParentRoutinePlanService
{
    private const int MaximumDateRangeDays = 62;
    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _tokenProtector;
    private readonly ILogger<ParentRoutinePlanService> _logger;

    public ParentRoutinePlanService(ApplicationDbContext db, IDataProtectionProvider dataProtectionProvider, ILogger<ParentRoutinePlanService> logger)
    {
        _db = db;
        _tokenProtector = dataProtectionProvider.CreateProtector("ParentRoutinePlanLink.Token.v1");
        _logger = logger;
    }

    public async Task<ParentRoutinePlanLinkStatusDto?> GetLinkStatusAsync(int athleteId, CancellationToken cancellationToken = default)
    {
        if (!await _db.Athletes.AsNoTracking().AnyAsync(a => a.AthleteId == athleteId, cancellationToken)) return null;
        var link = await ActiveLinkQuery(athleteId).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return ToStatus(link);
    }

    public async Task<ParentRoutinePlanLinkCreatedDto?> RotateLinkAsync(int athleteId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var athleteExists = await _db.Athletes.AnyAsync(a => a.AthleteId == athleteId && a.IsActive, cancellationToken);
            if (!athleteExists) return null;

            var now = DateTime.UtcNow;
            var activeLinks = await ActiveLinkQuery(athleteId).ToListAsync(cancellationToken);
            foreach (var activeLink in activeLinks)
            {
                activeLink.RevokedAtUtc = now;
                activeLink.RevokedByUserId = userId;
                activeLink.UpdatedDate = now;
                activeLink.UpdatedByUserId = userId;
            }

            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var link = new ParentRoutinePlanLink
            {
                AthleteId = athleteId,
                TokenHash = HashToken(rawToken),
                TokenHint = rawToken[^6..],
                ProtectedToken = _tokenProtector.Protect(rawToken),
                IsEnabled = true,
                CreatedDate = now,
                CreatedByUserId = userId,
            };
            _db.ParentRoutinePlanLinks.Add(link);
            await _db.SaveChangesAsync(cancellationToken);
            return new(rawToken, link.TokenHint, link.CreatedDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service: ParentRoutinePlanService Function: RotateLinkAsync UserId: {UserId} AthleteId: {AthleteId}", userId, athleteId);
            throw;
        }
    }

    public async Task<ParentRoutinePlanLinkStatusDto?> SetLinkAccessAsync(int athleteId, bool isEnabled, int userId, CancellationToken cancellationToken = default)
    {
        var link = await ActiveLinkQuery(athleteId).FirstOrDefaultAsync(cancellationToken);
        if (link is null) return null;
        link.IsEnabled = isEnabled;
        link.UpdatedDate = DateTime.UtcNow;
        link.UpdatedByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return ToStatus(link);
    }

    public async Task<ParentRoutinePlanCalendarDto?> GetCalendarAsync(string token, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (!IsValidRange(token, startDate, endDate)) return null;
        var link = await FindValidLinkAsync(token, cancellationToken);
        return link is null ? null : await BuildCalendarAsync(link.AthleteId, startDate, endDate, cancellationToken);
    }

    public async Task<ParentRoutinePlanCalendarDto?> SaveAsync(string token, DateOnly startDate, DateOnly endDate, IReadOnlyCollection<DateOnly> selectedDates, CancellationToken cancellationToken = default)
    {
        if (!IsValidRange(token, startDate, endDate)) return null;
        var distinctDates = selectedDates.Distinct().ToHashSet();
        if (distinctDates.Any(date => date < startDate || date > endDate))
            throw new ArgumentException("วันที่ที่เลือกต้องอยู่ภายในช่วงวันที่ที่กำลังบันทึก");

        var link = await FindValidLinkAsync(token, cancellationToken);
        if (link is null) return null;

        var availableDates = await _db.RoutineTrainingDates.AsNoTracking()
            .Where(item => item.TrainingDate >= startDate && item.TrainingDate <= endDate)
            .Select(item => item.TrainingDate)
            .ToListAsync(cancellationToken);
        var invalidDate = distinctDates.FirstOrDefault(date => !availableDates.Contains(date));
        if (invalidDate != default)
            throw new ArgumentException($"วันที่ {invalidDate:yyyy-MM-dd} ไม่ได้ถูกกำหนดเป็นวันฝึกซ้อมประจำ");

        var existingPlans = await _db.RoutineParticipationPlans.IgnoreQueryFilters()
            .Where(plan => plan.AthleteId == link.AthleteId && plan.TrainingDate >= startDate && plan.TrainingDate <= endDate)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var plan in existingPlans)
        {
            var selected = distinctDates.Contains(plan.TrainingDate);
            if (selected == !plan.IsDeleted) continue;
            plan.IsDeleted = !selected;
            plan.UpdatedDate = now;
        }

        var existingDates = existingPlans.Select(plan => plan.TrainingDate).ToHashSet();
        foreach (var date in distinctDates.Where(date => !existingDates.Contains(date)))
        {
            _db.RoutineParticipationPlans.Add(new RoutineParticipationPlan
            {
                AthleteId = link.AthleteId,
                TrainingDate = date,
                CreatedDate = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await BuildCalendarAsync(link.AthleteId, startDate, endDate, cancellationToken);
    }

    public async Task<IReadOnlyList<RoutineParticipationPlanSummaryDto>> GetSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ValidateRange(startDate, endDate);
        var availableDates = await _db.RoutineTrainingDates.AsNoTracking()
            .Where(item => item.TrainingDate >= startDate && item.TrainingDate <= endDate)
            .Select(item => item.TrainingDate).OrderBy(date => date).ToListAsync(cancellationToken);
        var counts = await _db.RoutineParticipationPlans.AsNoTracking()
            .Where(plan => plan.TrainingDate >= startDate && plan.TrainingDate <= endDate && plan.Athlete.IsActive)
            .GroupBy(plan => plan.TrainingDate)
            .Select(group => new { TrainingDate = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TrainingDate, item => item.Count, cancellationToken);
        return availableDates.Select(date => new RoutineParticipationPlanSummaryDto(date, counts.GetValueOrDefault(date))).ToList();
    }

    public async Task<IReadOnlyList<RoutineParticipationPlanAthleteDto>?> GetAthletesAsync(DateOnly trainingDate, CancellationToken cancellationToken = default)
    {
        var available = await _db.RoutineTrainingDates.AsNoTracking().AnyAsync(item => item.TrainingDate == trainingDate, cancellationToken);
        if (!available) return null;
        return await _db.RoutineParticipationPlans.AsNoTracking()
            .Where(plan => plan.TrainingDate == trainingDate && plan.Athlete.IsActive)
            .OrderBy(plan => plan.Athlete.AthleteCode)
            .Select(plan => new RoutineParticipationPlanAthleteDto(plan.AthleteId, plan.Athlete.AthleteCode, plan.Athlete.FullName, plan.Athlete.Nickname))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ParentRoutinePlanLink> ActiveLinkQuery(int athleteId) =>
        _db.ParentRoutinePlanLinks.Where(link => link.AthleteId == athleteId && link.RevokedAtUtc == null);

    private async Task<ParentRoutinePlanLink?> FindValidLinkAsync(string token, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(token);
        return await _db.ParentRoutinePlanLinks.AsNoTracking()
            .Include(link => link.Athlete)
            .FirstOrDefaultAsync(link => link.TokenHash == tokenHash && link.RevokedAtUtc == null && link.IsEnabled && link.Athlete.IsActive, cancellationToken);
    }

    private async Task<ParentRoutinePlanCalendarDto> BuildCalendarAsync(int athleteId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        var athlete = await _db.Athletes.AsNoTracking().SingleAsync(item => item.AthleteId == athleteId, cancellationToken);
        var availableDates = await _db.RoutineTrainingDates.AsNoTracking()
            .Where(item => item.TrainingDate >= startDate && item.TrainingDate <= endDate)
            .OrderBy(item => item.TrainingDate)
            .Select(item => item.TrainingDate)
            .ToListAsync(cancellationToken);
        var selected = await _db.RoutineParticipationPlans.AsNoTracking()
            .Where(plan => plan.AthleteId == athleteId && plan.TrainingDate >= startDate && plan.TrainingDate <= endDate)
            .Select(plan => plan.TrainingDate).ToListAsync(cancellationToken);
        var competitionMatches = await _db.CompetitionMatches.AsNoTracking()
            .Where(match => match.StartDate <= endDate && match.EndDate >= startDate)
            .OrderBy(match => match.StartDate)
            .ThenBy(match => match.Name)
            .Select(match => new PublicCompetitionMatchDto(match.Name, match.Province, match.StartDate, match.EndDate))
            .ToListAsync(cancellationToken);
        var selectedSet = selected.ToHashSet();
        var dates = availableDates
            .Select(date => new ParentRoutinePlanCalendarItemDto(date, selectedSet.Contains(date)))
            .ToList();
        return new(athlete.AthleteId, athlete.Nickname, athlete.FullName, dates, competitionMatches);
    }

    private ParentRoutinePlanLinkStatusDto ToStatus(ParentRoutinePlanLink? link)
    {
        string? token = null;
        if (!string.IsNullOrWhiteSpace(link?.ProtectedToken))
        {
            try { token = _tokenProtector.Unprotect(link.ProtectedToken); }
            catch (CryptographicException ex) { _logger.LogWarning(ex, "Unable to decrypt parent Routine plan token LinkId: {LinkId}", link.ParentRoutinePlanLinkId); }
        }
        return new(link is not null, link?.IsEnabled ?? false, token, link?.TokenHint, link?.CreatedDate);
    }

    private static bool IsValidRange(string token, DateOnly startDate, DateOnly endDate) =>
        !string.IsNullOrWhiteSpace(token) && endDate >= startDate && endDate.DayNumber - startDate.DayNumber <= MaximumDateRangeDays;

    private static void ValidateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate || endDate.DayNumber - startDate.DayNumber > MaximumDateRangeDays)
            throw new ArgumentException("ช่วงวันที่ต้องถูกต้องและไม่เกิน 63 วัน");
    }

    private static string HashToken(string rawToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
