using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Athlete Management (requirement.md 4.3, todo.md 4.2).</summary>
public class AthleteService : IAthleteService
{
    private const int SearchResultLimit = 20;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<AthleteService> _logger;

    public AthleteService(ApplicationDbContext db, ILogger<AthleteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResponse<AthleteListItemDto>> ListAsync(PagedRequest request)
    {
        var query = _db.Athletes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(a =>
                a.AthleteCode.ToUpper().Contains(search) ||
                a.FullName.ToUpper().Contains(search) ||
                (a.Nickname != null && a.Nickname.ToUpper().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(a => a.AthleteCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => MapToListItem(a))
            .ToListAsync();

        return new PagedResponse<AthleteListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<AthleteDetailDto?> GetByIdAsync(int athleteId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        return athlete is null ? null : MapToDetail(athlete);
    }

    public async Task<(AthleteDetailDto? Result, string? Error)> CreateAsync(AthleteCreateDto dto, int actionByUserId)
    {
        try
        {
            var codeTaken = await _db.Athletes.AnyAsync(a => a.AthleteCode == dto.AthleteCode);
            if (codeTaken)
            {
                return (null, "รหัสนักกีฬานี้มีอยู่ในระบบแล้ว");
            }

            var athlete = new Athlete
            {
                AthleteCode = dto.AthleteCode,
                FullName = dto.FullName,
                Nickname = dto.Nickname,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                ParentName = dto.ParentName,
                ParentPhoneNumber = dto.ParentPhoneNumber,
                AthleteLevel = dto.AthleteLevel,
                JoinDate = dto.JoinDate,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };

            _db.Athletes.Add(athlete);
            await _db.SaveChangesAsync();

            return (MapToDetail(athlete), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create athlete. Controller: AthletesController Service: AthleteService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<(AthleteDetailDto? Result, string? Error)> UpdateAsync(int athleteId, AthleteUpdateDto dto, int actionByUserId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        if (athlete is null)
        {
            return (null, null);
        }

        // Historical integrity (CLAUDE.md 4.5): editing profile fields never
        // touches past Attendance/PrivateSessionAthlete rows — those keep their
        // own snapshot (AthleteCodeSnapshot/AthleteNameSnapshot) taken at the time.
        athlete.FullName = dto.FullName;
        athlete.Nickname = dto.Nickname;
        athlete.DateOfBirth = dto.DateOfBirth;
        athlete.PhoneNumber = dto.PhoneNumber;
        athlete.ParentName = dto.ParentName;
        athlete.ParentPhoneNumber = dto.ParentPhoneNumber;
        athlete.AthleteLevel = dto.AthleteLevel;
        athlete.JoinDate = dto.JoinDate;
        athlete.Remarks = dto.Remarks;
        athlete.UpdatedByUserId = actionByUserId;
        athlete.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (MapToDetail(athlete), null);
    }

    public async Task<bool> SetStatusAsync(int athleteId, bool isActive, int actionByUserId)
    {
        var athlete = await _db.Athletes.FirstOrDefaultAsync(a => a.AthleteId == athleteId);
        if (athlete is null)
        {
            return false;
        }

        // FR-ATHLETE-003: deactivating never deletes or alters historical attendance records.
        athlete.IsActive = isActive;
        athlete.UpdatedByUserId = actionByUserId;
        athlete.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<AthleteOptionDto>> SearchActiveAsync(string? search)
    {
        var query = _db.Athletes.Where(a => a.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpper();
            query = query.Where(a =>
                a.AthleteCode.ToUpper().Contains(term) ||
                a.FullName.ToUpper().Contains(term) ||
                (a.Nickname != null && a.Nickname.ToUpper().Contains(term)));
        }

        return await query
            .OrderBy(a => a.FullName)
            .Take(SearchResultLimit)
            .Select(a => new AthleteOptionDto
            {
                AthleteId = a.AthleteId,
                AthleteCode = a.AthleteCode,
                FullName = a.FullName,
                Nickname = a.Nickname,
            })
            .ToListAsync();
    }

    private static AthleteListItemDto MapToListItem(Athlete athlete) => new()
    {
        AthleteId = athlete.AthleteId,
        AthleteCode = athlete.AthleteCode,
        FullName = athlete.FullName,
        Nickname = athlete.Nickname,
        DateOfBirth = athlete.DateOfBirth,
        AthleteLevel = athlete.AthleteLevel,
        IsActive = athlete.IsActive,
    };

    private static AthleteDetailDto MapToDetail(Athlete athlete) => new()
    {
        AthleteId = athlete.AthleteId,
        AthleteCode = athlete.AthleteCode,
        FullName = athlete.FullName,
        Nickname = athlete.Nickname,
        DateOfBirth = athlete.DateOfBirth,
        PhoneNumber = athlete.PhoneNumber,
        ParentName = athlete.ParentName,
        ParentPhoneNumber = athlete.ParentPhoneNumber,
        AthleteLevel = athlete.AthleteLevel,
        JoinDate = athlete.JoinDate,
        IsActive = athlete.IsActive,
        Remarks = athlete.Remarks,
    };
}
