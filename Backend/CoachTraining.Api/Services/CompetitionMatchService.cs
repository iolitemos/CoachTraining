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
        var query = _db.CompetitionMatches.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(match =>
                match.Name.ToUpper().Contains(search) ||
                match.Province.ToUpper().Contains(search));
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
        var match = await _db.CompetitionMatches.AsNoTracking()
            .FirstOrDefaultAsync(item => item.CompetitionMatchId == competitionMatchId);
        return match is null ? null : Map(match);
    }

    public async Task<CompetitionMatchDto> CreateAsync(CompetitionMatchRequestDto dto, int actionByUserId)
    {
        var match = new CompetitionMatch
        {
            Name = dto.Name.Trim(),
            Province = dto.Province.Trim(),
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate!.Value,
            CreatedByUserId = actionByUserId,
        };
        _db.CompetitionMatches.Add(match);
        await _db.SaveChangesAsync();
        return Map(match);
    }

    public async Task<CompetitionMatchDto?> UpdateAsync(int competitionMatchId, CompetitionMatchRequestDto dto, int actionByUserId)
    {
        var match = await _db.CompetitionMatches.FirstOrDefaultAsync(item => item.CompetitionMatchId == competitionMatchId);
        if (match is null) return null;

        match.Name = dto.Name.Trim();
        match.Province = dto.Province.Trim();
        match.StartDate = dto.StartDate!.Value;
        match.EndDate = dto.EndDate!.Value;
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
    };
}
