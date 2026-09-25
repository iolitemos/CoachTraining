using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.ParentRoutinePlans;

public record ParentRoutinePlanLinkStatusDto(bool Exists, bool IsEnabled, string? Token, string? TokenHint, DateTime? CreatedDate);
public record ParentRoutinePlanLinkCreatedDto(string Token, string TokenHint, DateTime CreatedDate);
public record ParentRoutinePlanAccessRequest(bool IsEnabled);
public record ParentRoutinePlanCalendarItemDto(DateOnly TrainingDate, bool IsSelected);
public record ParentRoutinePlanCalendarDto(int AthleteId, string? AthleteNickname, string AthleteFullName, IReadOnlyList<ParentRoutinePlanCalendarItemDto> Dates);
public record ParentRoutinePlanRangeRequest(DateOnly? StartDate, DateOnly? EndDate);
public class ParentRoutinePlanSaveRequest
{
    [Required] public DateOnly? StartDate { get; set; }
    [Required] public DateOnly? EndDate { get; set; }
    public List<DateOnly> SelectedDates { get; set; } = [];
}
public record RoutineParticipationPlanSummaryDto(DateOnly TrainingDate, int AthleteCount);
public record RoutineParticipationPlanAthleteDto(int AthleteId, string AthleteCode, string AthleteName, string? AthleteNickname);
