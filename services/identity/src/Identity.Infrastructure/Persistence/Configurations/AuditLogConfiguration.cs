using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd(); // bigserial

        builder.Property(a => a.ActionType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(a => a.ResourceId).HasMaxLength(60);
        builder.Property(a => a.Details).HasColumnType("jsonb");
        builder.Property(a => a.OccurredAt).IsRequired();

        // Sorgu deseni: "su kullanicinin son islemleri"
        builder.HasIndex(a => new { a.UserId, a.OccurredAt }).IsDescending(false, true);
    }
}
