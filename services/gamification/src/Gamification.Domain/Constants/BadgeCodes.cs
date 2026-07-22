namespace Gamification.Domain.Constants;

/// <summary>badges.code degerleri (case §6.2 rozet tablosu).</summary>
public static class BadgeCodes
{
    public const string IlkKampanya = "ILK_KAMPANYA";   // ilk optimizasyon
    public const string HizUstasi = "HIZ_USTASI";       // 2 saat altinda 10 optimizasyon
    public const string DonusumKrali = "DONUSUM_KRALI"; // 10 kampanyada hedef asimi
    public const string Maratoncu = "MARATONCU";        // bir gunde 20 optimizasyon
    public const string ChurnAvcisi = "CHURN_AVCISI";   // 10 RISKLI_KAYIP kurtarma
    public const string Uzman = "UZMAN";                // tek segmentte 50 optimizasyon
}
