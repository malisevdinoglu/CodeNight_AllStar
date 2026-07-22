using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamification.Infrastructure.Persistence.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.HasKey(b => b.Code);
        builder.Property(b => b.Code).HasMaxLength(30).ValueGeneratedNever();
        builder.Property(b => b.Name).HasMaxLength(60).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(200).IsRequired();
    }
}
