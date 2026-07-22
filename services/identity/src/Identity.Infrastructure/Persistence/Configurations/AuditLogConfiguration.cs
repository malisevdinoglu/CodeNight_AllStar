using System.Text.Json;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

/// <summary>Iskender.md §1 <c>audit_logs</c> — bigserial PK, jsonb details.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd().UseIdentityByDefaultColumn();

        builder.Property(a => a.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.OccurredAt).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(a => a.Success).IsRequired();
        builder.Property(a => a.ResourceId).HasMaxLength(60);

        // BUG FIX: AuditLog.Details entity'de duz metin (ornek: "USER_NOT_FOUND",
        // "failedLoginCount=3") ama kolon jsonb. Cevirici olmadan Npgsql bu metni
        // OLDUGU GIBI gonderir - "USER_NOT_FOUND" gecerli JSON degildir (tirnaksiz),
        // Postgres 22P02 ile reddeder. Sonuc: LOGIN_FAILED/ACCOUNT_LOCKED/403 gibi
        // TAM DA guvenlik testlerinde beklenen 401/403/423 yerine 500 donuyordu.
        // Cozum: duz string'i JSON string literaline serilestir/coz - tum cagiranlar
        // (LoginCommandHandler, VerifyOtp, Logout, CreateStaff, RefreshToken, 403
        // handler) DEGISMEDEN calisir, Details hala opak metin gibi davranir.
        builder.Property(a => a.Details)
            .HasColumnType("jsonb")
            .HasConversion(
                // NOT: options parametresi ZORUNLU olarak aciktan geciliyor - HasConversion lambda'lari
                // expression tree'ye derlenir, optional parametreli metod cagrisi expression tree icinde
                // YASAKTIR (CS0854), tum argumanlarin acikca verilmesi bunu asar.
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<string>(v, (JsonSerializerOptions?)null));

        builder.HasIndex(a => new { a.UserId, a.OccurredAt }).IsDescending(false, true);
    }
}
