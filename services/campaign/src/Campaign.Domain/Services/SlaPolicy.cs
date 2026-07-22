using Campaign.Domain.Enums;

namespace Campaign.Domain.Services;

/// <summary>Core_Principles §7: KRITIK 2 saat, YUKSEK 8 saat, ORTA 24 saat, DUSUK 72 saat.</summary>
public static class SlaPolicy
{
    public static TimeSpan GetDuration(CasePriority priority) => priority switch
    {
        CasePriority.KRITIK => TimeSpan.FromHours(2),
        CasePriority.YUKSEK => TimeSpan.FromHours(8),
        CasePriority.ORTA => TimeSpan.FromHours(24),
        CasePriority.DUSUK => TimeSpan.FromHours(72),
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
    };

    public static DateTimeOffset CalculateDeadline(CasePriority priority, DateTimeOffset createdAtUtc) =>
        createdAtUtc.Add(GetDuration(priority));

    /// <summary>Core_Principles §7: RISKLI_KAYIP segment -> minimum YUKSEK öncelik.</summary>
    public static CasePriority ApplyMinimumForSegment(CasePriority computed, SegmentType segment) =>
        segment == SegmentType.RISKLI_KAYIP && computed is CasePriority.DUSUK or CasePriority.ORTA
            ? CasePriority.YUKSEK
            : computed;

    public static int RemainingSeconds(DateTimeOffset deadline, DateTimeOffset nowUtc) =>
        Math.Max(0, (int)(deadline - nowUtc).TotalSeconds);
}
