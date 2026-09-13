using CoachTraining.Api.Models.Common;

namespace CoachTraining.Api.Models;

/// <summary>A shared note attached to one calendar date.</summary>
public class CalendarNote : AuditableEntity
{
    public int CalendarNoteId { get; set; }
    public DateOnly NoteDate { get; set; }
    public string Content { get; set; } = string.Empty;
}
