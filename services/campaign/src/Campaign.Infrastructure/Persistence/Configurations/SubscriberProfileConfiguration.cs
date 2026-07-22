using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class SubscriberProfileConfiguration : IEntityTypeConfiguration<SubscriberProfile>
{
    public void Configure(EntityTypeBuilder<SubscriberProfile> builder)
    {
        // PK dis kaynakli (Identity user id) — DB default uretilmez
        builder.HasKey(p => p.SubscriberId);
        builder.Property(p => p.SubscriberId).ValueGeneratedNever();

        builder.Property(p => p.GsmNumber).HasMaxLength(10).IsRequired();
        builder.Property(p => p.CurrentPlan).HasMaxLength(40).IsRequired();
        builder.Property(p => p.AvgMonthlyDataGb).HasPrecision(6, 2);
        builder.Property(p => p.MonthlySpendTl).HasPrecision(8, 2);
        builder.Property(p => p.PastAcceptanceRate).HasPrecision(3, 2);
        builder.Property(p => p.CurrentSegment).HasConversion<string>().HasMaxLength(20);
    }
}
