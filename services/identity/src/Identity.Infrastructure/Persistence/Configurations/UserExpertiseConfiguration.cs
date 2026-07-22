using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>Iskender.md §1 <c>user_expertises</c>.</summary>
public sealed class UserExpertiseConfiguration : IEntityTypeConfiguration<UserExpertise>
{
    public void Configure(EntityTypeBuilder<UserExpertise> builder)
    {
        builder.ToTable("user_expertises");

        builder.HasKey(e => e.Id);
        // BUG FIX: bkz. UserConfiguration.cs — Id domain'de client-side uretiliyor,
        // ValueGeneratedNever() gerekiyordu (ayni Added/Modified karisikligi riski).
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.SegmentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.SegmentType }).IsUnique();
    }
}
