using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Users;

namespace CoachTraining.Api.Services;

public interface IUserService
{
    Task<PagedResponse<UserListItemDto>> ListAsync(PagedRequest request);
    Task<UserDetailDto?> GetByIdAsync(int userId);
    Task<(UserDetailDto? Result, string? Error)> CreateAsync(UserCreateDto dto, int actionByUserId);
    Task<(UserDetailDto? Result, string? Error)> UpdateAsync(int userId, UserUpdateDto dto, int actionByUserId);
    Task<bool> SetStatusAsync(int userId, bool isActive, int actionByUserId);
    Task<(bool Success, string? Error)> AssignRolesAsync(int userId, List<int> roleIds, int actionByUserId);
    Task<(bool Success, string? Error)> SetCoachLinkAsync(int userId, int? coachId, int actionByUserId);
    Task<List<RoleOptionDto>> GetRoleOptionsAsync();
    Task<List<CoachOptionDto>> GetCoachOptionsAsync();
}
