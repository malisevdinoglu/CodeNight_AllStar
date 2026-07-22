using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Application.Common;

public interface ICampaignRepository
{
    void Add(CampaignEntity campaign);

    Task<CampaignEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CampaignEntity>> GetAllAsync(CancellationToken cancellationToken = default);
}
