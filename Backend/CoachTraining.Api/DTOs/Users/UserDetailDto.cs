namespace CoachTraining.Api.DTOs.Users;

public class UserDetailDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<int> RoleIds { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public int? CoachId { get; set; }
    public string? CoachCode { get; set; }
}
