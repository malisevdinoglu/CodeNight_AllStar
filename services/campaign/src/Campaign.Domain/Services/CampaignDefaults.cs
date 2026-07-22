using Campaign.Domain.Enums;

namespace Campaign.Domain.Services;

/// <summary>
/// Frontend'in basitleştirilmiş kampanya formu (title/type/targetSegment/description) indirim
/// oranı ve geçerlilik tarihlerini toplamaz — case'in gövdesi bunlara ihtiyaç duyduğundan
/// (üzerine kurulan Offer/Case akışı) makul iş varsayılanları burada tanımlanır.
/// </summary>
public static class CampaignDefaults
{
    private const int DefaultValidityDays = 30;

    public static decimal GetDefaultDiscountRate(CampaignType type) => type switch
    {
        CampaignType.EK_PAKET => 20m,
        CampaignType.TARIFE_YUKSELTME => 15m,
        CampaignType.CIHAZ_FIRSATI => 25m,
        CampaignType.SADAKAT => 30m,
        _ => 20m
    };

    public static DateOnly GetDefaultValidUntil(DateOnly validFrom) => validFrom.AddDays(DefaultValidityDays);

    /// <summary>
    /// Ortalama dönüşüm olasılığından öncelik türetimi (case dokümanında formül verilmez;
    /// makul, dokümante edilmiş bir eşik seçimi): düşük olasılık = daha çok müdahale gerekir.
    /// </summary>
    public static CasePriority DeterminePriorityFromConversion(decimal? avgConversionProbability)
    {
        if (avgConversionProbability is null)
        {
            return CasePriority.ORTA;
        }

        return avgConversionProbability switch
        {
            < 0.30m => CasePriority.KRITIK,
            < 0.50m => CasePriority.YUKSEK,
            < 0.70m => CasePriority.ORTA,
            _ => CasePriority.DUSUK
        };
    }
}
