namespace CoachTraining.Api.DTOs.Coaches;

/// <summary>Lightweight option used by dropdowns (e.g. User-Coach linking, future
/// Routine/Private Training coach selectors) — active coaches only.</summary>
public class CoachOptionDto
{
    public int CoachId { get; set; }
    public string CoachCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
