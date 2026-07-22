using Campaign.Domain.Entities;
using Campaign.Domain.Enums;

namespace Campaign.Domain.Services;

/// <summary>
/// Mali.md §"TAMAMLANDI'ya geçişte ... conversion_lift hesapla (basit: teklif kabul
/// oranındaki artış simülasyonu)". Gerçek bir A/B test altyapısı olmadığından (demo kapsamı),
/// AI'nin kampanya açılışında öngördüğü ortalama dönüşüm olasılığı "baseline" kabul edilir;
/// uzman optimizasyonu sonrası gerçekleşen kabul oranı ile karşılaştırılır. Sonuç yüzde
/// puanı cinsindendir (numeric(4,2) — Iskender.md §2).
/// </summary>
public static class ConversionLiftCalculator
{
    public static decimal? Calculate(IReadOnlyList<Offer> campaignOffers)
    {
        if (campaignOffers.Count == 0)
        {
            return null;
        }

        var actualAcceptanceRate = campaignOffers.Count(o => o.Status == OfferStatus.KABUL) / (decimal)campaignOffers.Count;
        var baselinePredictedRate = campaignOffers.Average(o => o.ConversionProbability);

        return Math.Round((actualAcceptanceRate - baselinePredictedRate) * 100m, 2);
    }
}
