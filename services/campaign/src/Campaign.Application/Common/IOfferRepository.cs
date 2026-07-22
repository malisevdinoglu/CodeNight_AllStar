using Campaign.Domain.Entities;

namespace Campaign.Application.Common;

public interface IOfferRepository
{
    void Add(Offer offer);

    /// <summary>Campaign navigasyonu dahil.</summary>
    Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Offer>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForCampaignAndSubscriberAsync(
        Guid campaignId, Guid subscriberId, CancellationToken cancellationToken = default);

    /// <summary>CaseDto.conversionProbability için: kampanyanın tekliflerinin ortalaması.</summary>
    Task<IReadOnlyList<Offer>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    /// <summary>Küçük ölçek demo verisi varsayımıyla dashboard conversionTrend agregasyonu için tam liste.</summary>
    Task<IReadOnlyList<Offer>> GetAllForDashboardAsync(CancellationToken cancellationToken = default);
}
