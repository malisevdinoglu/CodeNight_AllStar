using Gamification.Domain.Entities;

namespace Gamification.Application.Common;

/// <summary>UZMAN rozeti (tek segmentte 50 tamamlama) için (ExpertId, Segment) bazlı sayaç.</summary>
public interface ISegmentCompletionCountRepository
{
    void Add(SegmentCompletionCount row);

    Task<SegmentCompletionCount?> GetAsync(
        Guid expertId, string segment, CancellationToken cancellationToken = default);
}
