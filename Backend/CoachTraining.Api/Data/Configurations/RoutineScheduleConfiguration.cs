using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineScheduleConfiguration : IEntityTypeConfiguration<RoutineSchedule>
{
    public void Configure(EntityTypeBuilder<RoutineSchedule> builder)
    {
        builder.HasKey(rs => rs.RoutineScheduleId);

        builder.HasOne(rs => rs.Coach)
            .WithMany()
            .HasForeignKey(rs => rs.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports coach/date lookups used for schedule display and conflict checks.
        builder.HasIndex(rs => new { rs.CoachId, rs.EffectiveStartDate, rs.IsActive });

        builder.HasQueryFilter(rs => !rs.IsDeleted);
    }
}
