using Gamification.Domain.Constants;

namespace Gamification.Domain.Services;

/// <summary>
/// <c>campaign.optimized</c> event'i için tek bir noktada birleşik puan hesabı (Core_Principles §8:
/// TAMAMLANDI +10; süre &lt; 2 saat ise +5; conversion_lift &gt; hedef ise +15; KRITIK &amp; SLA
/// içinde +15). point_transactions.event_id UNIQUE olduğundan (İskender.md §3) her event TEK bir
/// satıra yazılır - alt bonuslar ayrı satırlar DEĞİL, toplam puana eklenir; hangi bonusların
/// tetiklendiği ExpertScore'un sayaç kolonlarında (FastCompletionCount vb.) ayrıca izlenir.
/// </summary>
public readonly record struct CampaignOptimizedPointResult(
    int TotalPoints,
    bool FastCompletion,
    bool TargetExceeded,
    bool KritikSlaBonusApplied);

public static class PointRuleEngine
{
    /// <param name="hadPriorSlaBreach">
    /// Bu case_id için daha önce SLA_ASIMI puan hareketi yazılmış mı (case.sla_breached zaten
    /// işlendi mi) - "KRITIK &amp; SLA içinde" bonusu SADECE hiç ihlal yaşanmamışsa uygulanır.
    /// </param>
    public static CampaignOptimizedPointResult CalculateCampaignOptimizedPoints(
        string priority,
        decimal? conversionLift,
        DateTimeOffset createdAt,
        DateTimeOffset completedAt,
        bool hadPriorSlaBreach)
    {
        var points = GamificationDefaults.BaseCompletionPoints;

        var fastCompletion = completedAt - createdAt < GamificationDefaults.FastCompletionThreshold;
        if (fastCompletion)
        {
            points += GamificationDefaults.FastCompletionBonus;
        }

        var targetExceeded = conversionLift is not null && conversionLift.Value > GamificationDefaults.TargetConversionLift;
        if (targetExceeded)
        {
            points += GamificationDefaults.TargetExceededBonus;
        }

        var kritikSlaBonusApplied = priority == "KRITIK" && !hadPriorSlaBreach;
        if (kritikSlaBonusApplied)
        {
            points += GamificationDefaults.KritikSlaBonus;
        }

        return new CampaignOptimizedPointResult(points, fastCompletion, targetExceeded, kritikSlaBonusApplied);
    }
}
