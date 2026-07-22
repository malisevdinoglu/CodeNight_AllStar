namespace Gamification.Domain.Constants;

/// <summary>point_transactions.reason degerleri (Core_Principles §8 puan tablosu).</summary>
public static class PointReasons
{
    public const string OptimizasyonTamamlandi = "OPTIMIZASYON_TAMAMLANDI"; // +10
    public const string HizBonusu = "HIZ_BONUSU";                           // +5  (< 2 saat)
    public const string HedefAsildi = "HEDEF_ASILDI";                       // +15
    public const string KritikSlaIcinde = "KRITIK_SLA_ICINDE";              // +15
    public const string SlaAsimi = "SLA_ASIMI";                             // -5
    public const string DusukPuan = "DUSUK_PUAN";                           // -3  (1-2 yildiz)
}
