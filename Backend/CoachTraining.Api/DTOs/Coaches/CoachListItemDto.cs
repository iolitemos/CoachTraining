namespace CoachTraining.Api.DTOs.Coaches;

public class CoachListItemDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public string? LinkedUsername { get; set; }
}
