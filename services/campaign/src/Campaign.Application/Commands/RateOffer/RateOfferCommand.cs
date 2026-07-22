using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Commands.RateOffer;

/// <summary>MUSTERI-only, sadece kendi teklifi. frontend OfferRateRequest ile birebir.</summary>
public sealed record RateOfferCommand(Guid OfferId, int Stars) : IRequest<OfferDto>;
