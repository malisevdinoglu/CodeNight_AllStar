using Gamification.Domain.Constants;

namespace Gamification.Domain.Services;

/// <summary>
/// Bir puan hareketinden sonraki güncel istatistikleri taşır (Mali.md §7: "her puan
/// hareketinden sonra koşulları kontrol et"). Application katmanı (repository sorgularıyla)
/// doldurur; bu tip saf veri taşıyıcıdır, I/O içermez.
/// </summary>
public sealed record BadgeEvaluationContext(
    int CompletedCaseCount,
    int FastCompletionCount,
    int TargetExceededCount,
    int RiskliKayipSavedCount,
    int TodayCompletedCount,
    int CurrentSegmentCompletedCount,
    IReadOnlySet<string> AlreadyEarnedBadgeCodes);

/// <summary>
/// Iskender.md §3 badge seed'iyle eşleşen 6 rozet kuralı (BadgeCodes). Saf fonksiyon:
/// aynı girdi → aynı çıktı, DB/zaman bağımlılığı yok - unit test edilebilir.
/// </summary>
public static class BadgeEvaluator
{
    public static IReadOnlyList<string> EvaluateNewlyEarned(BadgeEvaluationContext context)
    {
        var newlyEarned = new List<string>();

        void CheckAndAward(string badgeCode, bool conditionMet)
        {
            if (conditionMet && !context.AlreadyEarnedBadgeCodes.Contains(badgeCode))
            {
                newlyEarned.Add(badgeCode);
            }
        }

        CheckAndAward(BadgeCodes.IlkKampanya, context.CompletedCaseCount >= 1);
        CheckAndAward(BadgeCodes.HizUstasi, context.FastCompletionCount >= GamificationDefaults.HizUstasiThreshold);
        CheckAndAward(BadgeCodes.DonusumKrali, context.TargetExceededCount >= GamificationDefaults.DonusumKraliThreshold);
        CheckAndAward(BadgeCodes.Maratoncu, context.TodayCompletedCount >= GamificationDefaults.MaratoncuThreshold);
        CheckAndAward(BadgeCodes.ChurnAvcisi, context.RiskliKayipSavedCount >= GamificationDefaults.ChurnAvcisiThreshold);
        CheckAndAward(BadgeCodes.Uzman, context.CurrentSegmentCompletedCount >= GamificationDefaults.UzmanThreshold);

        return newlyEarned;
    }
}
