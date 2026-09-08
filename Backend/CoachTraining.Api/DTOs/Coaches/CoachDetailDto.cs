namespace CoachTraining.Api.DTOs.Coaches;

public class CoachDetailDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }

    /// <summary>Read-only — the link itself is managed from User & Role Management (todo.md 3.2/3.4).</summary>
    public int? LinkedUserId { get; set; }
    public string? LinkedUsername { get; set; }
}
