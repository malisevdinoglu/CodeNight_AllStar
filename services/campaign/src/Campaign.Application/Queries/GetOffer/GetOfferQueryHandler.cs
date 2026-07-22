using BuildingBlocks.Exceptions;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetOffer;

public sealed class GetOfferQueryHandler : IRequestHandler<GetOfferQuery, OfferDto>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferRatingRepository _offerRatingRepository;
    private readonly ICurrentRequestContext _requestContext;

    public GetOfferQueryHandler(
        IOfferRepository offerRepository,
        IOfferRatingRepository offerRatingRepository,
        ICurrentRequestContext requestContext)
    {
        _offerRepository = offerRepository;
        _offerRatingRepository = offerRatingRepository;
        _requestContext = requestContext;
    }

    public async Task<OfferDto> Handle(GetOfferQuery request, CancellationToken cancellationToken)
    {
        var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken)
            ?? throw new NotFoundException("OFFER_NOT_FOUND", "Teklif bulunamadi.");

        if (string.Equals(_requestContext.Role, "MUSTERI", StringComparison.Ordinal)
            && _requestContext.UserId != offer.SubscriberId)
        {
            throw new ForbiddenException("FORBIDDEN_OFFER_ACCESS", "Bu teklife erisim yetkiniz yok.");
        }

        var rating = await _offerRatingRepository.GetByOfferIdAsync(offer.Id, cancellationToken);
        return offer.ToDto(rating?.Stars);
    }
}
