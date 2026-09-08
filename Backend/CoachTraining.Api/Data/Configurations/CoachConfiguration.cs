using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.HasKey(c => c.CoachId);

        builder.Property(c => c.CoachCode).HasMaxLength(30).IsRequired();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Nickname).HasMaxLength(100);
        builder.Property(c => c.ColorHex).HasMaxLength(7).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.BankName).HasMaxLength(100);
        builder.Property(c => c.BankAccountNumber).HasMaxLength(30);
        builder.Property(c => c.BankAccountName).HasMaxLength(200);

        builder.HasIndex(c => c.CoachCode).IsUnique();

        builder.HasOne(c => c.User)
            .WithOne(u => u.Coach)
            .HasForeignKey<Coach>(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Preserves historical teaching records: deactivating/soft-deleting a
        // coach never deletes or rewrites past TrainingSession/Attendance rows
        // (FR-COACH-003) — those keep their own snapshot fields (NFR-002).
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
