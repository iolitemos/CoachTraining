using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineScheduleRequestConfiguration : IEntityTypeConfiguration<RoutineScheduleRequest>
{
    public void Configure(EntityTypeBuilder<RoutineScheduleRequest> builder)
    {
        builder.HasKey(x => x.RoutineScheduleRequestId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.ProposedRemarks).HasMaxLength(1000);
        builder.Property(x => x.ApprovedRemarks).HasMaxLength(1000);
        builder.Property(x => x.ReviewReason).HasMaxLength(1000);
        builder.HasOne(x => x.Coach).WithMany().HasForeignKey(x => x.CoachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RoutineSchedule).WithMany().HasForeignKey(x => x.RoutineScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.ProposedDate });
        builder.HasIndex(x => new { x.CoachId, x.ProposedDate });
        builder.HasIndex(x => x.SubmissionBatchId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
