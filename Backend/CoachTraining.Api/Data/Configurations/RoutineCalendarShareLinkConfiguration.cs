using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class RoutineCalendarShareLinkConfiguration : IEntityTypeConfiguration<RoutineCalendarShareLink>
{
    public void Configure(EntityTypeBuilder<RoutineCalendarShareLink> builder)
    {
        builder.HasKey(link => link.RoutineCalendarShareLinkId);
        builder.Property(link => link.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(link => link.TokenHint).HasMaxLength(8).IsRequired();
        builder.HasIndex(link => link.TokenHash).IsUnique();
        builder.HasIndex(link => new { link.RevokedAtUtc, link.IsDeleted });
        builder.HasQueryFilter(link => !link.IsDeleted);
    }
}
