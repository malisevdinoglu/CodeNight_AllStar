using Campaign.Domain.Enums;

namespace Campaign.Domain.Entities;

/// <summary>Dusuk donusumlu kampanyanin optimizasyon vakasi (case §4.2 state machine).</summary>
public class OptimizationCase
{
    public Guid Id { get; set; }

    /// <summary>OPT-2026-000045 (case_number_seq'ten uretilir).</summary>
    public string CaseNumber { get; set; } = null!;

    public Guid CampaignId { get; set; }
    public SegmentType Segment { get; set; }
    public CasePriority Priority { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.YENI;

    /// <summary>Identity'deki personel id — FK degil (cross-service).</summary>
    public Guid? AssignedExpertId { get; set; }

    /// <summary>created_at + SLA(priority). Sayac TAMAMLANDI'da durur.</summary>
    public DateTimeOffset SlaDeadline { get; set; }

    public bool SlaBreached { get; set; }

    /// <summary>TAMAMLANDI gecisinde zorunlu (state machine kosulu).</summary>
    public string? ExpertNote { get; set; }

    public decimal? ConversionLift { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Campaign Campaign { get; set; } = null!;
    public ICollection<CaseStatusHistory> StatusHistory { get; set; } = new List<CaseStatusHistory>();
}
