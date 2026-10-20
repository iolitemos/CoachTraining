using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CoachCompensationItemConfiguration : IEntityTypeConfiguration<CoachCompensationItem>
{
    public void Configure(EntityTypeBuilder<CoachCompensationItem> builder)
    {
        builder.HasKey(item => item.CoachCompensationItemId);
        builder.Property(item => item.CoachNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ParticipantNamesSnapshot).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.PrivateRateSnapshot).HasPrecision(18, 2);
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.ScheduledStartSnapshot).HasColumnType("timestamp without time zone");
        builder.Property(item => item.ScheduledEndSnapshot).HasColumnType("timestamp without time zone");

        builder.HasOne(item => item.Statement)
            .WithMany(statement => statement.Items)
            .HasForeignKey(item => item.CoachCompensationStatementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.TrainingSession)
            .WithOne(session => session.CompensationItem)
            .HasForeignKey<CoachCompensationItem>(item => item.TrainingSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.TrainingSessionId).IsUnique();
        builder.HasIndex(item => item.CoachCompensationStatementId);
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
