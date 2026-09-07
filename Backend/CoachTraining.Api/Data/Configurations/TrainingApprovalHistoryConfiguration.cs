using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class TrainingApprovalHistoryConfiguration : IEntityTypeConfiguration<TrainingApprovalHistory>
{
    public void Configure(EntityTypeBuilder<TrainingApprovalHistory> builder)
    {
        builder.HasKey(h => h.TrainingApprovalHistoryId);

        builder.Property(h => h.ActionType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(h => h.TrainingSession)
            .WithMany()
            .HasForeignKey(h => h.TrainingSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ActionByUser)
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.TrainingSessionId);

        // See TrainingSessionConfiguration — ActionByUser is a required link to
        // a soft-deletable User; approval history rows never change meaning
        // because the acting user later becomes inactive, so the EF warning is safe.
    }
}
