namespace CoachTraining.Api.DTOs.Athletes;

public class AthleteListItemDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? AthleteLevel { get; set; }
    public bool IsActive { get; set; }
}
