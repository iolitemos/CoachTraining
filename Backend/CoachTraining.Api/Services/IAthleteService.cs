using CoachTraining.Api.DTOs.Athletes;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.Services;

public interface IAthleteService
{
    Task<PagedResponse<AthleteListItemDto>> ListAsync(PagedRequest request, AthleteType athleteType);
    Task<AthleteDetailDto?> GetByIdAsync(int athleteId);
    Task<(AthleteDetailDto? Result, string? Error)> CreateAsync(AthleteCreateDto dto, int actionByUserId);
    Task<(AthleteDetailDto? Result, string? Error)> UpdateAsync(int athleteId, AthleteUpdateDto dto, int actionByUserId);
    Task<bool> SetStatusAsync(int athleteId, bool isActive, int actionByUserId);

    /// <summary>Active athletes matching a search term — shared by Routine attendance
    /// selection and Private Training assignment (todo.md 4.2).</summary>
    Task<List<AthleteOptionDto>> SearchActiveAsync(string? search);
}
