using Campaign.Domain.Enums;

namespace Campaign.Domain.Entities;

/// <summary>Aboneye sunulan kisisellestirilmis teklif. Skor 0.60 alti zaten yaratilmaz.</summary>
public class Offer
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }

    /// <summary>Identity'deki abone id — FK degil (cross-service).</summary>
    public Guid SubscriberId { get; set; }

    public decimal RecommendationScore { get; set; }
    public decimal ConversionProbability { get; set; }

    /// <summary>Skor &gt; 0.80 ise oncelikli gosterilir.</summary>
    public bool IsPriority { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.SUNULDU;
    public DateTimeOffset? RespondedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
}
