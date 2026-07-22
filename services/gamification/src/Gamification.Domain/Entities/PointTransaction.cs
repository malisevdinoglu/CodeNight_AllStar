namespace Gamification.Domain.Entities;

/// <summary>
/// Her puan hareketi tek satir. EventId UNIQUE = idempotency: ayni event
/// (RabbitMQ retry vb.) iki kez puanlanamaz.
/// </summary>
public class PointTransaction
{
    public long Id { get; set; }
    public Guid ExpertId { get; set; }
    public Guid EventId { get; set; }
    public string Reason { get; set; } = null!;

    /// <summary>+10 +5 +15 +15 -5 -3 (Core_Principles §8 puan kurallari).</summary>
    public int Points { get; set; }

    public Guid? CaseId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
