using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Commands.RespondToOffer;

/// <summary>MUSTERI-only, sadece kendi teklifi (IDOR: offer.subscriberId == token.sub).</summary>
public sealed record RespondToOfferCommand(Guid OfferId, OfferStatus Response) : IRequest<OfferDto>;
