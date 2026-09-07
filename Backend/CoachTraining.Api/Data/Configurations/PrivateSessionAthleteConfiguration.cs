using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class PrivateSessionAthleteConfiguration : IEntityTypeConfiguration<PrivateSessionAthlete>
{
    public void Configure(EntityTypeBuilder<PrivateSessionAthlete> builder)
    {
        builder.HasKey(psa => psa.PrivateSessionAthleteId);

        builder.Property(psa => psa.AthleteCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(psa => psa.AthleteNameSnapshot).HasMaxLength(200).IsRequired();

        // FR-PRIVATE / todo.md 2.6 — prevents duplicate athlete assignment within one session.
        builder.HasIndex(psa => new { psa.TrainingSessionId, psa.AthleteId }).IsUnique();

        // Supports "which Private sessions is this athlete assigned to" queries (todo.md 2.12).
        builder.HasIndex(psa => psa.AthleteId);

        builder.HasOne(psa => psa.TrainingSession)
            .WithMany(s => s.PrivateAthletes)
            .HasForeignKey(psa => psa.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(psa => psa.Athlete)
            .WithMany()
            .HasForeignKey(psa => psa.AthleteId)
            .OnDelete(DeleteBehavior.Restrict);

        // See TrainingSessionConfiguration: historical display must read
        // AthleteCodeSnapshot/AthleteNameSnapshot, not the live Athlete
        // navigation, so the expected EF soft-delete-filter warning is safe.
    }
}
