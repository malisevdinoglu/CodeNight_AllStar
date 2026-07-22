using Campaign.Domain.Enums;

namespace Campaign.Domain.Entities;

/// <summary>
/// Abone kullanim profili — AI oneri motorunun girdisi.
/// SubscriberId, Identity'deki user id ile AYNI degerdir ama FK DEGILDIR (cross-service).
/// </summary>
public class SubscriberProfile
{
    public Guid SubscriberId { get; set; }
    public string GsmNumber { get; set; } = null!;
    public string CurrentPlan { get; set; } = null!;
    public int TenureMonths { get; set; }
    public decimal AvgMonthlyDataGb { get; set; }
    public int AvgMonthlyCallMinutes { get; set; }
    public decimal MonthlySpendTl { get; set; }

    /// <summary>Son 6 ay ek paket alimi.</summary>
    public int PackagePurchaseCount { get; set; }

    public int ComplaintCount { get; set; }
    public int DaysSinceLastActivity { get; set; }

    /// <summary>0.00-1.00</summary>
    public decimal PastAcceptanceRate { get; set; }

    /// <summary>AI'nin son siniflandirmasi; hic siniflandirilmadiysa null.</summary>
    public SegmentType? CurrentSegment { get; set; }
}
