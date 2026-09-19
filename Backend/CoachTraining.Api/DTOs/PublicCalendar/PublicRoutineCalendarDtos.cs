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
    string CoachCode,
    string CoachNickname,
    string CoachColorHex,
    DateTime LatestUpdate);

public record PublicCompetitionMatchDto(
    string Name,
    string Province,
    DateOnly StartDate,
    DateOnly EndDate);

public record PublicCalendarNoteDto(DateOnly NoteDate, string Content, DateTime LatestUpdate);

public record PublicRoutineAttendanceItemDto(string AthleteName, int AttendanceCount);

public record PublicRoutineDailyAttendanceDto(DateOnly TrainingDate, IReadOnlyList<string> AthleteNicknames);

public record PublicRoutineCalendarDto(
    IReadOnlyList<PublicRoutineCalendarItemDto> Schedules,
    IReadOnlyList<PublicCompetitionMatchDto> CompetitionMatches,
    IReadOnlyList<PublicCalendarNoteDto> Notes,
    IReadOnlyList<PublicRoutineAttendanceItemDto> AttendanceSummary,
    IReadOnlyList<PublicRoutineDailyAttendanceDto> DailyAttendance);

public record RoutineCalendarShareStatusDto(
    bool Exists,
    bool IsEnabled,
    string? Token,
    string? TokenHint,
    DateTime? CreatedDate);

public record RoutineCalendarShareAccessRequest(bool IsEnabled);

public record RoutineCalendarShareCreatedDto(
    string Token,
    string TokenHint,
    DateTime CreatedDate);
