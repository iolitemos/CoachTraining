using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.CompetitionMatches;
using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class CompetitionMatchService : ICompetitionMatchService
{
    private readonly ApplicationDbContext _db;

    public CompetitionMatchService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<CompetitionMatchDto>> ListAsync(PagedRequest request)
    {
        IQueryable<CompetitionMatch> query = _db.CompetitionMatches.AsNoTracking().Include(match => match.Coaches);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(match =>
                match.Name.ToUpper().Contains(search) ||
                match.Province.ToUpper().Contains(search) ||
                match.Coaches.Any(coach => coach.CoachNameSnapshot.ToUpper().Contains(search) ||
                    (coach.CoachNicknameSnapshot != null && coach.CoachNicknameSnapshot.ToUpper().Contains(search))));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(match => match.StartDate)
            .ThenBy(match => match.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(match => Map(match))
            .ToListAsync();

        return new PagedResponse<CompetitionMatchDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<CompetitionMatchDto?> GetByIdAsync(int competitionMatchId)
    {
        var match = await _db.CompetitionMatches.AsNoTracking().Include(item => item.Coaches)
            .FirstOrDefaultAsync(item => item.CompetitionMatchId == competitionMatchId);
        return match is null ? null : Map(match);
    }

    public async Task<CompetitionMatchDto> CreateAsync(CompetitionMatchRequestDto dto, int actionByUserId)
    {
        var coaches = await GetActiveCoachesAsync(dto.CoachIds);
        var match = new CompetitionMatch
        {
            Name = dto.Name.Trim(),
            Province = dto.Province.Trim(),
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            CreatedByUserId = actionByUserId,
            Coaches = coaches.Select(coach => CreateAssignment(coach, actionByUserId)).ToList(),
        };
        _db.CompetitionMatches.Add(match);
        await _db.SaveChangesAsync();
        return Map(match);
    }

    public async Task<CompetitionMatchDto?> UpdateAsync(int competitionMatchId, CompetitionMatchRequestDto dto, int actionByUserId)
    {
        var match = await _db.CompetitionMatches.Include(item => item.Coaches).FirstOrDefaultAsync(item => item.CompetitionMatchId == competitionMatchId);
        if (match is null) return null;

        var coaches = await GetActiveCoachesAsync(dto.CoachIds);

        match.Name = dto.Name.Trim();
        match.Province = dto.Province.Trim();
        match.StartDate = dto.StartDate!.Value;
        match.EndDate = dto.EndDate!.Value;
        _db.CompetitionMatchCoaches.RemoveRange(match.Coaches);
        match.Coaches = coaches.Select(coach => CreateAssignment(coach, actionByUserId)).ToList();
        match.UpdatedDate = DateTime.UtcNow;
        match.UpdatedByUserId = actionByUserId;
        await _db.SaveChangesAsync();
        return Map(match);
    }

    public async Task<bool> DeleteAsync(int competitionMatchId, int actionByUserId)
    {
        var match = await _db.CompetitionMatches.FirstOrDefaultAsync(item => item.CompetitionMatchId == competitionMatchId);
        if (match is null) return false;

        match.IsDeleted = true;
        match.UpdatedDate = DateTime.UtcNow;
        match.UpdatedByUserId = actionByUserId;
        await _db.SaveChangesAsync();
        return true;
    }

    private static CompetitionMatchDto Map(CompetitionMatch match) => new()
    {
        CompetitionMatchId = match.CompetitionMatchId,
        Name = match.Name,
        Province = match.Province,
        StartDate = match.StartDate,
        EndDate = match.EndDate,
        Coaches = match.Coaches.OrderBy(item => item.CoachNicknameSnapshot ?? item.CoachNameSnapshot)
            .Select(item => new CompetitionMatchCoachDto(item.CoachId, item.CoachNameSnapshot, item.CoachNicknameSnapshot))
            .ToList(),
    };

    private async Task<List<Coach>> GetActiveCoachesAsync(IEnumerable<int> coachIds)
    {
        var distinctIds = coachIds.Distinct().ToList();
        var coaches = await _db.Coaches.Where(coach => distinctIds.Contains(coach.CoachId) && coach.IsActive).ToListAsync();
        if (coaches.Count != distinctIds.Count)
            throw new ArgumentException("พบโค้ชที่ไม่มีอยู่หรือไม่ได้เปิดใช้งาน");
        return coaches;
    }

    private static CompetitionMatchCoach CreateAssignment(Coach coach, int actionByUserId) => new()
    {
        CoachId = coach.CoachId,
        CoachNameSnapshot = coach.FullName,
        CoachNicknameSnapshot = coach.Nickname,
        CreatedByUserId = actionByUserId,
    };
}
