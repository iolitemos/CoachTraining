using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasKey(a => a.AttendanceId);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.AthleteCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(a => a.AthleteNameSnapshot).HasMaxLength(200).IsRequired();

        // FR-RATT-004 / FR-PATT-005 / todo.md 2.7 — prevents duplicate attendance per athlete/session.
        builder.HasIndex(a => new { a.TrainingSessionId, a.AthleteId }).IsUnique();

        // Supports athlete attendance history/report queries (todo.md 2.12).
        builder.HasIndex(a => a.AthleteId);

        builder.HasOne(a => a.TrainingSession)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.TrainingSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Athlete)
            .WithMany()
            .HasForeignKey(a => a.AthleteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PrivateSessionAthleteId).IsUnique();
        builder.HasOne(a => a.PrivateSessionAthlete)
            .WithMany()
            .HasForeignKey(a => a.PrivateSessionAthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.RecordedByUser)
            .WithMany()
            .HasForeignKey(a => a.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // See TrainingSessionConfiguration: historical display must read
        // AthleteCodeSnapshot/AthleteNameSnapshot, not the live Athlete/User
        // navigations, so the expected EF soft-delete-filter warning is safe.
    }
}
