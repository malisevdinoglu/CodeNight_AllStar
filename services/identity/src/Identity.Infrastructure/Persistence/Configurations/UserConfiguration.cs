using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>Iskender.md §1 <c>users</c> — Fluent API (data annotation YASAK, Core_Principles §1).</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        // BUG FIX: Id domain'de client-side Guid.NewGuid() ile uretiliyor (User.cs), ama burada
        // ValueGeneratedOnAdd() DB/EF'in uretecegini soyluyordu. Guid PK icin EF'in reachability
        // tabanli state tahmini soyle calisir: key CLR default'a (Guid.Empty) esitse Added,
        // degilse (Guid.NewGuid() hep dolu deger uretir) mevcut kayit varsayilip Modified/Unchanged
        // isaretlenir. Bu, navigation uzerinden (explicit Add() OLMADAN) parent'a eklenen entity'ler
        // icin (login sirasinda user.IssueRefreshToken -> _refreshTokens.Add(...)) INSERT yerine
        // hicbir satiri etkilemeyen bir UPDATE'e -> DbUpdateConcurrencyException'a yol aciyordu
        // (canli testte login'in HER zaman 500 donmesiyle dogrulandi). ValueGeneratedNever()
        // EF'e "anahtari her zaman uygulama atar" der; artik key dolu olsa da Added dogru
        // isaretlenir. DB kolonu zaten uuid NOT NULL (store-generated default yok), sema degismiyor.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.FirstName).HasMaxLength(60).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(60).IsRequired();
        builder.Property(u => u.GsmNumber).HasMaxLength(10);
        builder.Property(u => u.Email).HasMaxLength(120);
        builder.Property(u => u.PasswordHash).HasMaxLength(200);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.Region).HasMaxLength(30);

        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(u => u.FailedLoginCount).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.GsmNumber).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.Expertises)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.Expertises))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
