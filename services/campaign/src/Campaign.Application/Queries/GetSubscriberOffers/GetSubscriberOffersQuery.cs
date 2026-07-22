using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetSubscriberOffers;

/// <summary>GET /subscribers/:id/offers. IDOR: MUSTERI sadece kendi subscriberId'si icin cagirabilir.</summary>
public sealed record GetSubscriberOffersQuery(Guid SubscriberId) : IRequest<IReadOnlyList<OfferDto>>;
