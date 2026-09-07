using CoachTraining.Api.Data;
using CoachTraining.Api.DTOs.Coaches;
using CoachTraining.Api.DTOs.Common;
using CoachTraining.Api.DTOs.Users;
using CoachTraining.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachTraining.Api.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ICoachService _coachService;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext db, IPasswordHasher<User> passwordHasher, ICoachService coachService, ILogger<UserService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _coachService = coachService;
        _logger = logger;
    }

    public async Task<PagedResponse<UserListItemDto>> ListAsync(PagedRequest request)
    {
        var query = _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Coach)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpper();
            query = query.Where(u =>
                u.Username.ToUpper().Contains(search) ||
                u.FullName.ToUpper().Contains(search) ||
                u.Email.ToUpper().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => MapToListItem(u))
            .ToListAsync();

        return new PagedResponse<UserListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<UserDetailDto?> GetByIdAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Coach)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        return user is null ? null : MapToDetail(user);
    }

    public async Task<(UserDetailDto? Result, string? Error)> CreateAsync(UserCreateDto dto, int actionByUserId)
    {
        try
        {
            var usernameTaken = await _db.Users.AnyAsync(u => u.Username == dto.Username);
            if (usernameTaken)
            {
                return (null, "ชื่อผู้ใช้นี้มีอยู่ในระบบแล้ว");
            }

            var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailTaken)
            {
                return (null, "อีเมลนี้มีอยู่ในระบบแล้ว");
            }

            var roles = await _db.Roles.Where(r => dto.RoleIds.Contains(r.RoleId)).ToListAsync();
            if (roles.Count != dto.RoleIds.Distinct().Count())
            {
                return (null, "พบบทบาทที่ไม่ถูกต้อง");
            }

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                IsActive = true,
                CreatedByUserId = actionByUserId,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            user.UserRoles = roles.Select(r => new UserRole { Role = r, CreatedByUserId = actionByUserId }).ToList();

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return (MapToDetail(user), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user. Controller: UsersController Service: UserService Function: CreateAsync ActionByUserId: {ActionByUserId}", actionByUserId);
            throw;
        }
    }

    public async Task<(UserDetailDto? Result, string? Error)> UpdateAsync(int userId, UserUpdateDto dto, int actionByUserId)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Coach)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
        {
            return (null, null);
        }

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
        if (emailTaken)
        {
            return (null, "อีเมลนี้มีอยู่ในระบบแล้ว");
        }

        user.Email = dto.Email;
        user.FullName = dto.FullName;
        user.UpdatedByUserId = actionByUserId;
        user.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (MapToDetail(user), null);
    }

    public async Task<bool> SetStatusAsync(int userId, bool isActive, int actionByUserId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        user.UpdatedByUserId = actionByUserId;
        user.UpdatedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> AssignRolesAsync(int userId, List<int> roleIds, int actionByUserId)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            return (false, null);
        }

        var roles = await _db.Roles.Where(r => roleIds.Contains(r.RoleId)).ToListAsync();
        if (roles.Count != roleIds.Distinct().Count())
        {
            return (false, "พบบทบาทที่ไม่ถูกต้อง");
        }

        _db.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles = roles.Select(r => new UserRole { RoleId = r.RoleId, UserId = userId, CreatedByUserId = actionByUserId }).ToList();

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetCoachLinkAsync(int userId, int? coachId, int actionByUserId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            return (false, null);
        }

        if (coachId is not null)
        {
            var coach = await _db.Coaches.FirstOrDefaultAsync(c => c.CoachId == coachId);
            if (coach is null)
            {
                return (false, "ไม่พบข้อมูลโค้ชที่เลือก");
            }

            var alreadyLinked = await _db.Coaches.AnyAsync(c => c.CoachId == coachId && c.UserId != null && c.UserId != userId);
            if (alreadyLinked)
            {
                return (false, "โค้ชคนนี้เชื่อมโยงกับบัญชีผู้ใช้อื่นอยู่แล้ว");
            }

            coach.UserId = userId;
            coach.UpdatedByUserId = actionByUserId;
            coach.UpdatedDate = DateTime.UtcNow;
        }
        else
        {
            var currentCoach = await _db.Coaches.FirstOrDefaultAsync(c => c.UserId == userId);
            if (currentCoach is not null)
            {
                currentCoach.UserId = null;
                currentCoach.UpdatedByUserId = actionByUserId;
                currentCoach.UpdatedDate = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<RoleOptionDto>> GetRoleOptionsAsync() =>
        await _db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleOptionDto { RoleId = r.RoleId, Name = r.Name })
            .ToListAsync();

    public Task<List<CoachOptionDto>> GetCoachOptionsAsync() => _coachService.GetActiveOptionsAsync();

    private static UserListItemDto MapToListItem(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        CoachCode = user.Coach?.CoachCode,
    };

    private static UserDetailDto MapToDetail(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        CoachId = user.Coach?.CoachId,
        CoachCode = user.Coach?.CoachCode,
    };
}
