using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CalendarNoteConfiguration : IEntityTypeConfiguration<CalendarNote>
{
    public void Configure(EntityTypeBuilder<CalendarNote> builder)
    {
        builder.HasKey(note => note.CalendarNoteId);
        builder.Property(note => note.Content).HasMaxLength(1000).IsRequired();
        builder.HasIndex(note => note.NoteDate).IsUnique();
        builder.HasQueryFilter(note => !note.IsDeleted);
    }
}
