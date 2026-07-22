using Campaign.Domain.Entities;
using Campaign.Domain.Enums;

namespace Campaign.Application.Common;

public interface ISubscriberProfileRepository
{
    Task<SubscriberProfile?> GetByIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>CreateCampaignCommand: hedef segmentteki abone havuzu.</summary>
    Task<IReadOnlyList<SubscriberProfile>> GetBySegmentAsync(
        SegmentType segment, CancellationToken cancellationToken = default);

    void UpsertCurrentSegment(Guid subscriberId, SegmentType segment);
}
