using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(o => o.RecommendationScore).HasPrecision(3, 2);
        builder.Property(o => o.ConversionProbability).HasPrecision(3, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(15).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        // Ayni kampanya ayni aboneye iki kez sunulmaz
        builder.HasIndex(o => new { o.CampaignId, o.SubscriberId }).IsUnique();
        builder.HasIndex(o => o.SubscriberId);

        builder.HasOne(o => o.Campaign)
            .WithMany()
            .HasForeignKey(o => o.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
