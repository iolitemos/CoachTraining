namespace CoachTraining.Api.DTOs.Athletes;

public class AthleteDetailDto
{
    public int AthleteId { get; set; }
    public string AthleteCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ParentName { get; set; }
    public string? ParentPhoneNumber { get; set; }
    public string? AthleteLevel { get; set; }
    public DateOnly? JoinDate { get; set; }
    public bool IsActive { get; set; }
    public string? Remarks { get; set; }
}
