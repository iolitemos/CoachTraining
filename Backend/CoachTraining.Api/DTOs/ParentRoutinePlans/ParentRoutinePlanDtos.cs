using System.ComponentModel.DataAnnotations;
using CoachTraining.Api.DTOs.PublicCalendar;
using CoachTraining.Api.Models.Enums;

namespace CoachTraining.Api.DTOs.ParentRoutinePlans;

public record ParentRoutinePlanLinkStatusDto(bool Exists, bool IsEnabled, string? Token, string? TokenHint, DateTime? CreatedDate);
public record ParentRoutinePlanLinkCreatedDto(string Token, string TokenHint, DateTime CreatedDate);
public record ParentRoutinePlanAccessRequest(bool IsEnabled);
public record ParentRoutinePlanCalendarItemDto(DateOnly TrainingDate, bool IsSelected);
public record ParentPrivateSessionCalendarItemDto(
    DateOnly SessionDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string CoachNickname,
    string? Location,
    SessionStatus Status);
public record ParentRoutinePlanCalendarDto(
    int AthleteId,
    string? AthleteNickname,
    string AthleteFullName,
    IReadOnlyList<ParentRoutinePlanCalendarItemDto> Dates,
    IReadOnlyList<PublicCompetitionMatchDto> CompetitionMatches,
    IReadOnlyList<ParentPrivateSessionCalendarItemDto> PrivateSessions);
public record ParentRoutinePlanRangeRequest(DateOnly? StartDate, DateOnly? EndDate);
public class ParentRoutinePlanSaveRequest
{
    [Required] public DateOnly? StartDate { get; set; }
    [Required] public DateOnly? EndDate { get; set; }
    public List<DateOnly> SelectedDates { get; set; } = [];
}
public record RoutineParticipationPlanSummaryDto(DateOnly TrainingDate, int AthleteCount);
public record RoutineParticipationPlanAthleteDto(int AthleteId, string AthleteCode, string AthleteName, string? AthleteNickname);
