using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class ConflictOverrideHistoryConfiguration : IEntityTypeConfiguration<ConflictOverrideHistory>
{
    public void Configure(EntityTypeBuilder<ConflictOverrideHistory> builder)
    {
        builder.HasKey(h => h.ConflictOverrideHistoryId);

        builder.Property(h => h.ConflictType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(h => h.Reason).IsRequired();

        builder.HasOne(h => h.TrainingSession)
            .WithMany()
            .HasForeignKey(h => h.TrainingSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.RoutineSchedule)
            .WithMany()
            .HasForeignKey(h => h.RoutineScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ActionByUser)
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // See TrainingSessionConfiguration — ActionByUser is a required link to
        // a soft-deletable User; override history rows never change meaning
        // because the acting user later becomes inactive, so the EF warning is safe.
    }
}
