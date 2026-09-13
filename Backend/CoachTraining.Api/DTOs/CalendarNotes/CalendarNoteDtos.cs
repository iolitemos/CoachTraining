using System.ComponentModel.DataAnnotations;

namespace CoachTraining.Api.DTOs.CalendarNotes;

public record CalendarNoteDto(int CalendarNoteId, DateOnly NoteDate, string Content);

public class CalendarNoteRangeRequest
{
    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }
}

public class CalendarNoteUpsertRequest
{
    [Required(ErrorMessage = "กรุณากรอก Note")]
    [MaxLength(1000, ErrorMessage = "Note ต้องมีความยาวไม่เกิน 1,000 ตัวอักษร")]
    public string Content { get; set; } = string.Empty;
}
