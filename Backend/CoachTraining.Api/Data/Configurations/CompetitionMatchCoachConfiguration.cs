using CoachTraining.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoachTraining.Api.Data.Configurations;

public class CompetitionMatchCoachConfiguration : IEntityTypeConfiguration<CompetitionMatchCoach>
{
    public void Configure(EntityTypeBuilder<CompetitionMatchCoach> builder)
    {
        builder.HasKey(item => item.CompetitionMatchCoachId);
        builder.Property(item => item.CoachNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(item => item.CoachNicknameSnapshot).HasMaxLength(100);
        builder.HasIndex(item => new { item.CompetitionMatchId, item.CoachId }).IsUnique();
        builder.HasIndex(item => item.CoachId);

        builder.HasOne(item => item.CompetitionMatch)
            .WithMany(match => match.Coaches)
            .HasForeignKey(item => item.CompetitionMatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Coach)
            .WithMany()
            .HasForeignKey(item => item.CoachId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
