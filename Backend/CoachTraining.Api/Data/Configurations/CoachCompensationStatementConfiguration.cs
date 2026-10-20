using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CoachCompensationStatementConfiguration : IEntityTypeConfiguration<CoachCompensationStatement>
{
    public void Configure(EntityTypeBuilder<CoachCompensationStatement> builder)
    {
        builder.HasKey(statement => statement.CoachCompensationStatementId);
        builder.Property(statement => statement.BaseSalarySnapshot).HasPrecision(18, 2);
        builder.Property(statement => statement.AdjustmentAmount).HasPrecision(18, 2);
        builder.Property(statement => statement.AdjustmentReason).HasMaxLength(500);
        builder.Property(statement => statement.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(statement => statement.Coach)
            .WithMany()
            .HasForeignKey(statement => statement.CoachId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(statement => new { statement.CoachId, statement.Year, statement.Month }).IsUnique();
        builder.HasIndex(statement => new { statement.Year, statement.Month, statement.Status });
        builder.HasQueryFilter(statement => !statement.IsDeleted);
    }
}
