using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CoachSubstitutionHistoryConfiguration : IEntityTypeConfiguration<CoachSubstitutionHistory>
{
    public void Configure(EntityTypeBuilder<CoachSubstitutionHistory> builder)
    {
        builder.HasKey(h => h.CoachSubstitutionHistoryId);

        builder.Property(h => h.Reason).IsRequired();

        builder.HasOne(h => h.TrainingSession)
            .WithMany()
            .HasForeignKey(h => h.TrainingSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.OriginalCoach)
            .WithMany()
            .HasForeignKey(h => h.OriginalCoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.SubstituteCoach)
            .WithMany()
            .HasForeignKey(h => h.SubstituteCoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ActionByUser)
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.TrainingSessionId);

        // See TrainingSessionConfiguration — Coach/User links are required
        // references to soft-deletable master data; substitution history rows
        // never change meaning when a coach/user later becomes inactive, so the
        // EF soft-delete-filter warning is safe.
    }
}
