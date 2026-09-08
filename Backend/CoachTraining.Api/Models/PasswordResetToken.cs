using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

public class PasswordResetToken : AuditableEntity
{
    public int PasswordResetTokenId { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? InvalidatedAtUtc { get; set; }
    public User User { get; set; } = null!;
}
