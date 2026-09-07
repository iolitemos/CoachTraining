namespace CoachTraining.Api.DTOs.Common;

/// <summary>
/// Shared date-range filter used by dashboard and report endpoints.
/// </summary>
public class DateRangeFilter
{
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
