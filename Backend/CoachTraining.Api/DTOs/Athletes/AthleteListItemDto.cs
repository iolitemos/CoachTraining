namespace CoachTraining.Api.DTOs.Athletes;

using CoachTraining.Api.Models.Enums;

public class AthleteListItemDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public AthleteType AthleteType { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? AthleteLevel { get; set; }
    public bool IsActive { get; set; }
}
