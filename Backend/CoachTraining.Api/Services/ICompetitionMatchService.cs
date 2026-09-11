using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.CompetitionMatches;

namespace CoachTraining.Api.Services;

public interface ICompetitionMatchService
{
    Task<PagedResponse<CompetitionMatchDto>> ListAsync(PagedRequest request);
    Task<CompetitionMatchDto?> GetByIdAsync(int competitionMatchId);
    Task<CompetitionMatchDto> CreateAsync(CompetitionMatchRequestDto dto, int actionByUserId);
    Task<CompetitionMatchDto?> UpdateAsync(int competitionMatchId, CompetitionMatchRequestDto dto, int actionByUserId);
    Task<bool> DeleteAsync(int competitionMatchId, int actionByUserId);
}
