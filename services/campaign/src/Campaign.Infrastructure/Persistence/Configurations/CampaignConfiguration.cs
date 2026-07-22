using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<CampaignEntity>
{
    public void Configure(EntityTypeBuilder<CampaignEntity> builder)
    {
        builder.ToTable("campaigns");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CampaignNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.CampaignNumber).IsUnique();

        builder.Property(c => c.Title).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.TargetSegment).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.DiscountRate).HasPrecision(5, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
