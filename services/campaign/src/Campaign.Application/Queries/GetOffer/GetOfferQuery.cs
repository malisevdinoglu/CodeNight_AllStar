using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetOffer;

public sealed record GetOfferQuery(Guid OfferId) : IRequest<OfferDto>;
