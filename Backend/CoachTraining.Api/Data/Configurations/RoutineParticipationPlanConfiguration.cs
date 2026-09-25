using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineParticipationPlanConfiguration : IEntityTypeConfiguration<RoutineParticipationPlan>
{
    public void Configure(EntityTypeBuilder<RoutineParticipationPlan> builder)
    {
        builder.HasKey(plan => plan.RoutineParticipationPlanId);
        builder.HasIndex(plan => new { plan.AthleteId, plan.TrainingDate }).IsUnique();
        builder.HasIndex(plan => plan.TrainingDate);
        builder.HasOne(plan => plan.Athlete).WithMany().HasForeignKey(plan => plan.AthleteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(plan => !plan.IsDeleted);
    }
}
