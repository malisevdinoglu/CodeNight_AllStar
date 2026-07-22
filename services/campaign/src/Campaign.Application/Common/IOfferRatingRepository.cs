using Campaign.Domain.Entities;

namespace Campaign.Application.Common;

public interface IOfferRatingRepository
{
    void Add(OfferRating rating);

    Task<bool> ExistsForOfferAsync(Guid offerId, CancellationToken cancellationToken = default);

    Task<OfferRating?> GetByOfferIdAsync(Guid offerId, CancellationToken cancellationToken = default);
}
