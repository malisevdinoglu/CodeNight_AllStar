namespace Gamification.Domain.Entities;

/// <summary>
/// Uzmanin toplam puan ve rozet sayaclari. ExpertId, Identity'deki user id — FK degil.
/// DisplayName event'ten denormalize edilir (Identity'ye REST cagrisi yapilmaz — event tabanli mimari).
/// </summary>
public class ExpertScore
{
    public Guid ExpertId { get; set; }
    public string DisplayName { get; set; } = null!;
    public int TotalPoints { get; set; }
    public int CompletedCaseCount { get; set; }

    /// <summary>2 saatin altinda tamamlanan vaka sayisi (Hiz Ustasi rozeti).</summary>
    public int FastCompletionCount { get; set; }

    /// <summary>Donusum hedefi asilan vaka sayisi (Donusum Krali rozeti).</summary>
    public int TargetExceededCount { get; set; }

    /// <summary>Kurtarilan RISKLI_KAYIP vakasi (Churn Avcisi rozeti).</summary>
    public int RiskliKayipSavedCount { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
