using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gamification.Infrastructure.Persistence.Configurations;

public class PointTransactionConfiguration : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd(); // bigserial

        builder.Property(t => t.Reason).HasMaxLength(40).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        // Idempotency: ayni event iki kez puanlanamaz (RabbitMQ redelivery korumasi)
        builder.HasIndex(t => t.EventId).IsUnique();

        // Sorgu deseni: profil ekrani "son puan hareketleri"
        builder.HasIndex(t => new { t.ExpertId, t.CreatedAt });
    }
}
