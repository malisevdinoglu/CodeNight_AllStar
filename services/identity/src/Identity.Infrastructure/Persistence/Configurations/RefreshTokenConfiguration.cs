using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TokenHash);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Rotation zinciri: eski token yeni token'i isaret eder; zincir kirilmasin diye Restrict
        builder.HasOne(t => t.ReplacedBy)
            .WithMany()
            .HasForeignKey(t => t.ReplacedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);
    }
}
