using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campaign.Infrastructure.Persistence.Configurations;

public class OfferRatingConfiguration : IEntityTypeConfiguration<OfferRating>
{
    public void Configure(EntityTypeBuilder<OfferRating> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("ck_offer_ratings_stars", "stars BETWEEN 1 AND 5"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.CreatedAt).IsRequired();

        // Tek seferlik puanlama garantisi — ikinci deneme unique ihlali → 409
        builder.HasIndex(r => r.OfferId).IsUnique();

        builder.HasOne(r => r.Offer)
            .WithMany()
            .HasForeignKey(r => r.OfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
