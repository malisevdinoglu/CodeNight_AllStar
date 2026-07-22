namespace Gamification.Domain.Constants;

/// <summary>
/// Core_Principles §8 puan tablosu + Mali.md §7 rozet eşikleri - dokümante edilmiş sabitler.
/// "Hedef dönüşüm artışı" (conversion_lift eşiği) case dokümanında sayısal olarak verilmez;
/// burada 5.0 (yüzde puanı) olarak sabitlenir - Campaign.Domain.Services.ConversionLiftCalculator
/// çıktısı da yüzde puanı cinsindendir, birim tutarlı.
/// </summary>
public static class GamificationDefaults
{
    public const int BaseCompletionPoints = 10;
    public const int FastCompletionBonus = 5;
    public const int TargetExceededBonus = 15;
    public const int KritikSlaBonus = 15;
    public const int SlaBreachPenalty = -5;
    public const int LowRatingPenalty = -3;

    public static readonly TimeSpan FastCompletionThreshold = TimeSpan.FromHours(2);

    /// <summary>conversion_lift bu değeri AŞARSA (yüzde puanı) HEDEF_ASILDI bonusu tetiklenir.</summary>
    public const decimal TargetConversionLift = 5.0m;

    public const int HizUstasiThreshold = 10;
    public const int DonusumKraliThreshold = 10;
    public const int MaratoncuThreshold = 20;
    public const int ChurnAvcisiThreshold = 10;
    public const int UzmanThreshold = 50;

    public const int BronzMax = 499;
    public const int GumusMax = 1499;
    public const int AltinMax = 2999;
}
