using BuildingBlocks.Exceptions;
using BuildingBlocks.Messaging;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.Events;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using MassTransit;
using MediatR;

namespace Campaign.Application.Commands.RateOffer;

/// <summary>
/// Case §"OfferId UNIQUE = tek seferlik puanlama garantisi": ikinci deneme 409. Puanlanan
/// uzman, teklifin kampanyasına bağlı OptimizationCase'in AssignedExpertId'sinden çözülür
/// (Offer'da doğrudan bir uzman referansı yoktur - 1 kampanya = 1 case varsayımı).
/// </summary>
public sealed class RateOfferCommandHandler : IRequestHandler<RateOfferCommand, OfferDto>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferRatingRepository _offerRatingRepository;
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public RateOfferCommandHandler(
        IOfferRepository offerRepository,
        IOfferRatingRepository offerRatingRepository,
        IOptimizationCaseRepository caseRepository,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _offerRepository = offerRepository;
        _offerRatingRepository = offerRatingRepository;
        _caseRepository = caseRepository;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OfferDto> Handle(RateOfferCommand request, CancellationToken cancellationToken)
    {
        if (_requestContext.UserId is not { } callerId)
        {
            throw new ForbiddenException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");
        }

        var offer = await _offerRepository.GetByIdAsync(request.OfferId, cancellationToken)
            ?? throw new NotFoundException("OFFER_NOT_FOUND", "Teklif bulunamadi.");

        if (offer.SubscriberId != callerId)
        {
            throw new ForbiddenException("FORBIDDEN_OFFER_ACCESS", "Bu teklife erisim yetkiniz yok.");
        }

        if (offer.Status != OfferStatus.KABUL)
        {
            throw new DomainRuleException("OFFER_NOT_ACCEPTED", "Sadece kabul edilen teklifler puanlanabilir.");
        }

        if (await _offerRatingRepository.ExistsForOfferAsync(offer.Id, cancellationToken))
        {
            throw new ConflictException("OFFER_ALREADY_RATED", "Bu teklif zaten puanlanmis.");
        }

        var now = _dateTimeProvider.UtcNow;
        var rating = new OfferRating
        {
            Id = Guid.NewGuid(),
            OfferId = offer.Id,
            SubscriberId = callerId,
            Stars = (short)request.Stars,
            CreatedAt = now,
        };
        _offerRatingRepository.Add(rating);

        var relatedCase = await _caseRepository.GetByCampaignIdAsync(offer.CampaignId, cancellationToken);

        await _publishEndpoint.PublishIntegrationEventAsync(
            new OfferRatedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new OfferRatedPayload(
                    offer.Id, offer.SubscriberId, relatedCase?.AssignedExpertId, offer.CampaignId, request.Stars),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return offer.ToDto(request.Stars);
    }
}
