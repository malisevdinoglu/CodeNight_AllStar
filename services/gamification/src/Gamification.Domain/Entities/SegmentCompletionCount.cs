namespace Gamification.Domain.Entities;

/// <summary>Segment basina tamamlama sayaci (UZMAN rozeti: tek segmentte 50).</summary>
public class SegmentCompletionCount
{
    public Guid ExpertId { get; set; }

    /// <summary>Event'ten gelen segment string'i (YUKSEK_DEGER vb.) — enum bagimliligi yok.</summary>
    public string Segment { get; set; } = null!;

    public int CompletedCount { get; set; }
}
