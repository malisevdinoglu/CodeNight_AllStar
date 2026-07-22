using Campaign.Domain.Enums;

namespace Campaign.Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; }

    /// <summary>Benzersiz, okunabilir: CMP-2026-000123 (campaign_number_seq'ten uretilir).</summary>
    public string CampaignNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    /// <summary>Mali Faz 5 ek alanı (frontend CreateCampaignRequest.description) — opsiyonel.</summary>
    public string? Description { get; set; }

    public CampaignType Type { get; set; }
    public SegmentType TargetSegment { get; set; }

    /// <summary>Yuzde cinsinden indirim orani.</summary>
    public decimal DiscountRate { get; set; }

    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidUntil { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.AKTIF;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
