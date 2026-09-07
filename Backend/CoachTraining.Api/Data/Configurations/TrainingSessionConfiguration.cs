using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> builder)
    {
        builder.HasKey(s => s.TrainingSessionId);

        builder.Property(s => s.TrainingType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.AssignedCoachCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(s => s.AssignedCoachNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ActualCoachCodeSnapshot).HasMaxLength(30);
        builder.Property(s => s.ActualCoachNameSnapshot).HasMaxLength(200);
        builder.Property(s => s.Location).HasMaxLength(200);

        // Scheduled/actual times are the gym's local wall-clock time (this system
        // has one venue/timezone — requirement.md has no multi-timezone concept),
        // built from DateOnly + TimeOnly with DateTime.Kind=Unspecified. Map them
        // as "timestamp without time zone" rather than the timestamptz Npgsql
        // would otherwise pick for DateTime, which requires Kind=Utc.
        builder.Property(s => s.ScheduledStartDateTime).HasColumnType("timestamp without time zone");
        builder.Property(s => s.ScheduledEndDateTime).HasColumnType("timestamp without time zone");
        builder.Property(s => s.ActualStartDateTime).HasColumnType("timestamp without time zone");
        builder.Property(s => s.ActualEndDateTime).HasColumnType("timestamp without time zone");

        builder.HasOne(s => s.RoutineSchedule)
            .WithMany()
            .HasForeignKey(s => s.RoutineScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.AssignedCoach)
            .WithMany()
            .HasForeignKey(s => s.AssignedCoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ActualCoach)
            .WithMany()
            .HasForeignKey(s => s.ActualCoachId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reschedule link: original <-> replacement (FR-SESSION-010, FR-CR-006).
        builder.HasOne(s => s.OriginalSession)
            .WithMany(s => s.ReplacementSessions)
            .HasForeignKey(s => s.OriginalSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dashboard/report and coach-schedule-conflict queries.
        builder.HasIndex(s => new { s.AssignedCoachId, s.SessionDate });
        builder.HasIndex(s => new { s.ActualCoachId, s.SessionDate });
        builder.HasIndex(s => s.SessionDate);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.TrainingType);

        // Deliberately no global soft-delete query filter here: Cancelled and
        // Rescheduled are explicit statuses that must stay visible in normal
        // history/report queries (requirement.md FR-SESSION-009, FR-CR-003).
        // IsDeleted is reserved for rare administrative data corrections.
        //
        // EF logs a model-validation warning because AssignedCoach/RoutineSchedule
        // are required relationships to entities that DO have a soft-delete
        // filter (Coach, RoutineSchedule) — this is expected: historical display
        // must read the *Snapshot fields above, not live navigation properties,
        // precisely so a later coach deactivation never changes past session
        // display (NFR-002). Services that intentionally need the live related
        // row for a soft-deleted Coach/RoutineSchedule should call
        // IgnoreQueryFilters() explicitly.
    }
}
