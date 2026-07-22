using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence.Repositories;

public sealed class OfferRatingRepository : IOfferRatingRepository
{
    private readonly CampaignDbContext _dbContext;

    public OfferRatingRepository(CampaignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(OfferRating rating) => _dbContext.OfferRatings.Add(rating);

    public Task<bool> ExistsForOfferAsync(Guid offerId, CancellationToken cancellationToken = default) =>
        _dbContext.OfferRatings.AnyAsync(r => r.OfferId == offerId, cancellationToken);

    public Task<OfferRating?> GetByOfferIdAsync(Guid offerId, CancellationToken cancellationToken = default) =>
        _dbContext.OfferRatings.FirstOrDefaultAsync(r => r.OfferId == offerId, cancellationToken);
}
