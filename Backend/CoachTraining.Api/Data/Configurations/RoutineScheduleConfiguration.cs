using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineScheduleConfiguration : IEntityTypeConfiguration<RoutineSchedule>
{
    public void Configure(EntityTypeBuilder<RoutineSchedule> builder)
    {
        builder.HasKey(rs => rs.RoutineScheduleId);

        builder.Property(rs => rs.Name).HasMaxLength(200).IsRequired();
        builder.Property(rs => rs.RecurrencePattern).HasMaxLength(50).IsRequired();

        builder.HasOne(rs => rs.Coach)
            .WithMany()
            .HasForeignKey(rs => rs.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports the routine-recurrence lookup used when generating sessions.
        builder.HasIndex(rs => new { rs.CoachId, rs.DayOfWeek, rs.IsActive });

        builder.HasQueryFilter(rs => !rs.IsDeleted);
    }
}
