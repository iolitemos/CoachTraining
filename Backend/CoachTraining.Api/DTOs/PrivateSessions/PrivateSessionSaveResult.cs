using CoachTraining.Api.DTOs.Conflicts;

namespace CoachTraining.Api.DTOs.PrivateSessions;

public class PrivateSessionSaveResult
{
    public PrivateSessionDetailDto? Session { get; set; }
    public string? Error { get; set; }
    public List<ConflictDetail> Conflicts { get; set; } = [];
}
