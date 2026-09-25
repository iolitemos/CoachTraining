using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineTrainingDateConfiguration : IEntityTypeConfiguration<RoutineTrainingDate>
{
    public void Configure(EntityTypeBuilder<RoutineTrainingDate> builder)
    {
        builder.HasKey(item => item.RoutineTrainingDateId);
        builder.HasIndex(item => item.TrainingDate).IsUnique();
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
