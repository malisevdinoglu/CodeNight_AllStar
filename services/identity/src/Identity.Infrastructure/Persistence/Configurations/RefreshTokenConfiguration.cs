using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>Iskender.md §1 <c>refresh_tokens</c> — rotation zinciri <see cref="RefreshToken.ReplacedById"/>.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);
        // BUG FIX: bkz. UserConfiguration.cs ayni satir aciklamasi — Id RefreshToken.Issue()
        // icinde client-side Guid.NewGuid() ile uretiliyor, ValueGeneratedNever() gerekiyordu.
        // Bu tam olarak login'in 500 dondugu satirdi: yeni token izlenen User'in
        // RefreshTokens navigation'ina eklenip SaveChanges'te UPDATE ... WHERE id=<yeniGuid>
        // olarak gonderiliyor, 0 satir etkileniyordu.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.ExpiresAt).IsRequired();

        // replaced_by_id -> refresh_tokens (self-referencing, kısıtlayıcı olmayan; theft zincirinde silme kısıtına gerek yok)
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ReplacedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.TokenHash);
    }
}
