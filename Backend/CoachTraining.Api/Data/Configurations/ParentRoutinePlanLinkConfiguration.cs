using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class ParentRoutinePlanLinkConfiguration : IEntityTypeConfiguration<ParentRoutinePlanLink>
{
    public void Configure(EntityTypeBuilder<ParentRoutinePlanLink> builder)
    {
        builder.HasKey(link => link.ParentRoutinePlanLinkId);
        builder.Property(link => link.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(link => link.TokenHint).HasMaxLength(8).IsRequired();
        builder.Property(link => link.ProtectedToken).HasMaxLength(2000);
        builder.HasIndex(link => link.TokenHash).IsUnique();
        builder.HasIndex(link => new { link.AthleteId, link.RevokedAtUtc });
        builder.HasOne(link => link.Athlete).WithMany().HasForeignKey(link => link.AthleteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(link => !link.IsDeleted);
    }
}
