using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class TrainingLogConfiguration : IEntityTypeConfiguration<TrainingLog>
{
    public void Configure(EntityTypeBuilder<TrainingLog> builder)
    {
        builder.HasKey(l => l.TrainingLogId);

        // One training log per session.
        builder.HasIndex(l => l.TrainingSessionId).IsUnique();

        builder.HasOne(l => l.TrainingSession)
            .WithOne(s => s.TrainingLog)
            .HasForeignKey<TrainingLog>(l => l.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
