using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> builder)
    {
        builder.HasKey(a => a.AthleteId);

        builder.Property(a => a.AthleteCode).HasMaxLength(30).IsRequired();
        builder.Property(a => a.FullName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Nickname).HasMaxLength(100);
        builder.Property(a => a.PhoneNumber).HasMaxLength(30);
        builder.Property(a => a.ParentName).HasMaxLength(200);
        builder.Property(a => a.ParentPhoneNumber).HasMaxLength(30);
        builder.Property(a => a.AthleteLevel).HasMaxLength(100);

        builder.HasIndex(a => a.AthleteCode).IsUnique();

        // Preserves historical attendance: deactivating/soft-deleting an
        // athlete never deletes or rewrites past Attendance rows
        // (FR-ATHLETE-003) — those keep their own snapshot fields (NFR-002).
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
