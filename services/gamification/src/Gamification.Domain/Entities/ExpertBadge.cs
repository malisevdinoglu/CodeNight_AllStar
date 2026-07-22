namespace Gamification.Domain.Entities;

/// <summary>Kazanilan rozet (composite PK: ExpertId + BadgeCode — ayni rozet iki kez kazanilmaz).</summary>
public class ExpertBadge
{
    public Guid ExpertId { get; set; }
    public string BadgeCode { get; set; } = null!;
    public DateTimeOffset EarnedAt { get; set; }

    public Badge Badge { get; set; } = null!;
}
