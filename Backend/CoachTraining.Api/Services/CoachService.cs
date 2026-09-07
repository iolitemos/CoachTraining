using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

/// <summary>Coach Management (requirement.md 4.2, todo.md 4.1).</summary>
public class CoachService : ICoachService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CoachService> _logger;

    public CoachService(ApplicationDbContext db, ILogger<CoachService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResponse<CoachListItemDto>> ListAsync(PagedRequest request)
    {
        var query = _db.Coaches.Include(c => c.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(c =>
                c.CoachCode.ToUpper().Contains(search) ||
                c.FullName.ToUpper().Contains(search) ||
                (c.Nickname != null && c.Nickname.ToUpper().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.CoachCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => MapToListItem(c))
            .ToListAsync();

        return new PagedResponse<CoachListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<CoachDetailDto?> GetByIdAsync(int coachId)
    {
        var coach = await _db.Coaches.Include(c => c.User).FirstOrDefaultAsync(c => c.CoachId == coachId);
        return coach is null ? null : MapToDetail(coach);
    }

    public async Task<(CoachDetailDto? Result, string? Error)> CreateAsync(CoachCreateDto dto, int actionByUserId)
    {
        try
        {
            var codeTaken = await _db.Coaches.AnyAsync(c => c.CoachCode == dto.CoachCode);
            if (codeTaken)
            {
                return (null, "รหัสโค้ชนี้มีอยู่ในระบบแล้ว");
            }

            var coach = new Coach
            {
                CoachCode = dto.CoachCode,
                FullName = dto.FullName,
                Nickname = dto.Nickname,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                CoachType = dto.CoachType,
                Specialization = dto.Specialization,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };

            _db.Coaches.Add(coach);
            await _db.SaveChangesAsync();

            return (MapToDetail(coach), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create coach. Controller: CoachesController Service: CoachService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<(CoachDetailDto? Result, string? Error)> UpdateAsync(int coachId, CoachUpdateDto dto, int actionByUserId)
    {
        var coach = await _db.Coaches.Include(c => c.User).FirstOrDefaultAsync(c => c.CoachId == coachId);
        if (coach is null)
        {
            return (null, null);
        }

        // Historical integrity (CLAUDE.md 4.5): editing profile fields never
        // touches past TrainingSession rows — those keep their own snapshot
        // (AssignedCoachCodeSnapshot/AssignedCoachNameSnapshot) taken at the time.
        coach.FullName = dto.FullName;
        coach.Nickname = dto.Nickname;
        coach.PhoneNumber = dto.PhoneNumber;
        coach.Email = dto.Email;
        coach.CoachType = dto.CoachType;
        coach.Specialization = dto.Specialization;
        coach.Remarks = dto.Remarks;
        coach.UpdatedByUserId = actionByUserId;
        coach.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (MapToDetail(coach), null);
    }

    public async Task<bool> SetStatusAsync(int coachId, bool isActive, int actionByUserId)
    {
        var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == coachId);
        if (coach is null)
        {
            return false;
        }

        // FR-COACH-003: deactivating never deletes or alters historical teaching records.
        coach.IsActive = isActive;
        coach.UpdatedByUserId = actionByUserId;
        coach.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CoachOptionDto>> GetActiveOptionsAsync() =>
        await _db.Coaches
            .Where(c => c.IsActive)
            .OrderBy(c => c.FullName)
            .Select(c => new CoachOptionDto { CoachId = c.CoachId, CoachCode = c.CoachCode, FullName = c.FullName })
            .ToListAsync();

    private static CoachListItemDto MapToListItem(Coach coach) => new()
    {
        CoachId = coach.CoachId,
        CoachCode = coach.CoachCode,
        FullName = coach.FullName,
        Nickname = coach.Nickname,
        PhoneNumber = coach.PhoneNumber,
        Email = coach.Email,
        CoachType = coach.CoachType,
        IsActive = coach.IsActive,
        LinkedUsername = coach.User != null ? coach.User.Username : null,
    };

    private static CoachDetailDto MapToDetail(Coach coach) => new()
    {
        CoachId = coach.CoachId,
        CoachCode = coach.CoachCode,
        FullName = coach.FullName,
        Nickname = coach.Nickname,
        PhoneNumber = coach.PhoneNumber,
        Email = coach.Email,
        CoachType = coach.CoachType,
        Specialization = coach.Specialization,
        IsActive = coach.IsActive,
        Remarks = coach.Remarks,
        LinkedUserId = coach.UserId,
        LinkedUsername = coach.User != null ? coach.User.Username : null,
    };
}
