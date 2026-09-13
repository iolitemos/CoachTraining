using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.PublicCalendar;

public class PublicRoutineCalendarRequest
{
    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }
}

public record PublicRoutineCalendarItemDto(
    DateOnly TrainingDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string CoachNickname,
    string CoachColorHex);

public record PublicCompetitionMatchDto(
    string Name,
    string Province,
    DateOnly StartDate,
    DateOnly EndDate);

public record PublicRoutineCalendarDto(
    IReadOnlyList<PublicRoutineCalendarItemDto> Schedules,
    IReadOnlyList<PublicCompetitionMatchDto> CompetitionMatches);

public record RoutineCalendarShareStatusDto(
    bool IsActive,
    string? TokenHint,
    DateTime? CreatedDate);

public record RoutineCalendarShareCreatedDto(
    string Token,
    string TokenHint,
    DateTime CreatedDate);
