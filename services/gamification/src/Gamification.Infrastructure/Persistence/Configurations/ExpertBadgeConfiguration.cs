using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamification.Infrastructure.Persistence.Configurations;

public class ExpertBadgeConfiguration : IEntityTypeConfiguration<ExpertBadge>
{
    public void Configure(EntityTypeBuilder<ExpertBadge> builder)
    {
        builder.HasKey(b => new { b.ExpertId, b.BadgeCode });
        builder.Property(b => b.BadgeCode).HasMaxLength(30);
        builder.Property(b => b.EarnedAt).IsRequired();

        builder.HasOne(b => b.Badge)
            .WithMany()
            .HasForeignKey(b => b.BadgeCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
