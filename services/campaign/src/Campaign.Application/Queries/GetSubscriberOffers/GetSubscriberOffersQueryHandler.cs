using BuildingBlocks.Exceptions;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetSubscriberOffers;

public sealed class GetSubscriberOffersQueryHandler : IRequestHandler<GetSubscriberOffersQuery, IReadOnlyList<OfferDto>>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferRatingRepository _offerRatingRepository;
    private readonly ICurrentRequestContext _requestContext;

    public GetSubscriberOffersQueryHandler(
        IOfferRepository offerRepository,
        IOfferRatingRepository offerRatingRepository,
        ICurrentRequestContext requestContext)
    {
        _offerRepository = offerRepository;
        _offerRatingRepository = offerRatingRepository;
        _requestContext = requestContext;
    }

    public async Task<IReadOnlyList<OfferDto>> Handle(GetSubscriberOffersQuery request, CancellationToken cancellationToken)
    {
        // MUSTERI sadece kendi tekliflerini gorebilir; personel/supervizor destek amacli herhangi
        // bir abonenin tekliflerini gorebilir (Api katmaninda role izinli olarak dogrulanir).
        if (string.Equals(_requestContext.Role, "MUSTERI", StringComparison.Ordinal)
            && _requestContext.UserId != request.SubscriberId)
        {
            throw new ForbiddenException("FORBIDDEN_SUBSCRIBER_ACCESS", "Baska bir abonenin tekliflerini goruntuleyemezsiniz.");
        }

        var offers = await _offerRepository.GetBySubscriberIdAsync(request.SubscriberId, cancellationToken);

        var dtos = new List<OfferDto>();
        foreach (var offer in offers)
        {
            var rating = await _offerRatingRepository.GetByOfferIdAsync(offer.Id, cancellationToken);
            dtos.Add(offer.ToDto(rating?.Stars));
        }

        return dtos;
    }
}
