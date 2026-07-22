using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamification.Infrastructure.Persistence.Configurations;

public class ExpertScoreConfiguration : IEntityTypeConfiguration<ExpertScore>
{
    public void Configure(EntityTypeBuilder<ExpertScore> builder)
    {
        // PK dis kaynakli (Identity user id) — DB default uretilmez
        builder.HasKey(s => s.ExpertId);
        builder.Property(s => s.ExpertId).ValueGeneratedNever();

        builder.Property(s => s.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(s => s.TotalPoints).HasDefaultValue(0);
        builder.Property(s => s.CompletedCaseCount).HasDefaultValue(0);
        builder.Property(s => s.FastCompletionCount).HasDefaultValue(0);
        builder.Property(s => s.TargetExceededCount).HasDefaultValue(0);
        builder.Property(s => s.RiskliKayipSavedCount).HasDefaultValue(0);
    }
}
