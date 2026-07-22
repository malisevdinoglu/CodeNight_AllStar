using Campaign.Domain.Enums;

namespace Campaign.Domain.Entities;

/// <summary>Her state gecisi buraya yazilir (audit + SLA analizi).</summary>
public class CaseStatusHistory
{
    public long Id { get; set; }
    public Guid CaseId { get; set; }
    public CaseStatus FromStatus { get; set; }
    public CaseStatus ToStatus { get; set; }

    /// <summary>Kullanici id'si veya sistem gecisleri icin SystemUserId sentineli.</summary>
    public Guid ChangedBy { get; set; }

    public string? Note { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public OptimizationCase Case { get; set; } = null!;

    /// <summary>Sistem (AI/zamanlayici) gecisleri icin sentinel — tum sifirlar.</summary>
    public static readonly Guid SystemUserId = Guid.Empty;
}
