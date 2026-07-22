using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence.Repositories;

public sealed class OfferRepository : IOfferRepository
{
    private readonly CampaignDbContext _dbContext;

    public OfferRepository(CampaignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<Offer> WithCampaign() => _dbContext.Offers.Include(o => o.Campaign);

    public void Add(Offer offer) => _dbContext.Offers.Add(offer);

    public Task<Offer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithCampaign().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Offer>> GetBySubscriberIdAsync(Guid subscriberId, CancellationToken cancellationToken = default) =>
        await WithCampaign()
            .Where(o => o.SubscriberId == subscriberId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForCampaignAndSubscriberAsync(
        Guid campaignId, Guid subscriberId, CancellationToken cancellationToken = default) =>
        _dbContext.Offers.AnyAsync(o => o.CampaignId == campaignId && o.SubscriberId == subscriberId, cancellationToken);

    public async Task<IReadOnlyList<Offer>> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        await _dbContext.Offers.Where(o => o.CampaignId == campaignId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Offer>> GetAllForDashboardAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Offers.ToListAsync(cancellationToken);
}
