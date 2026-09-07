using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.AuditLogId);

        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();

        builder.HasOne(a => a.ActionByUser)
            .WithMany()
            .HasForeignKey(a => a.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        // See TrainingSessionConfiguration — ActionByUser is a required link to
        // a soft-deletable User; audit rows never change meaning because the
        // acting user later becomes inactive, so the EF warning is safe.
    }
}
