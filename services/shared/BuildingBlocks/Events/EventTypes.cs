namespace BuildingBlocks.Events;

/// <summary>
/// Core_Principles §8 event kataloğunun TEK doğru kaynağı — magic string
/// tekrarını önler. Somut event sınıfları (Faz 5/7) <c>EventType</c>
/// property'sinde bu sabitleri kullanır.
/// </summary>
public static class EventTypes
{
    public const string CampaignCreated = "campaign.created";
    public const string CaseCreated = "case.created";
    public const string CaseAssigned = "case.assigned";
    public const string CaseStatusChanged = "case.status_changed";
    public const string CampaignOptimized = "campaign.optimized";
    public const string CaseSlaBreached = "case.sla_breached";
    public const string SegmentOverridden = "segment.overridden";
    public const string OfferResponded = "offer.responded";
    public const string OfferRated = "offer.rated";
    public const string BadgeEarned = "badge.earned";
    public const string PointsUpdated = "points.updated";
}
