using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CompetitionMatchConfiguration : IEntityTypeConfiguration<CompetitionMatch>
{
    public void Configure(EntityTypeBuilder<CompetitionMatch> builder)
    {
        builder.HasKey(match => match.CompetitionMatchId);
        builder.Property(match => match.Name).HasMaxLength(200).IsRequired();
        builder.Property(match => match.Province).HasMaxLength(100).IsRequired();
        builder.HasIndex(match => match.StartDate);
        builder.HasIndex(match => match.EndDate);
        builder.HasQueryFilter(match => !match.IsDeleted);
    }
}
