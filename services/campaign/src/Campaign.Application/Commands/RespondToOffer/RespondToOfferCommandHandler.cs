using BuildingBlocks.Exceptions;
using BuildingBlocks.Messaging;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.Events;
using Campaign.Domain.Enums;
using MassTransit;
using MediatR;

namespace Campaign.Application.Commands.RespondToOffer;

/// <summary>
/// Core_Principles §6 IDOR kontrolü: offer.subscriberId != token.sub ise 403 (kimlik dogrulanmis
/// ama BASKA abonenin teklifine yanit vermeye calisiyor). offer.responded event'ini AI consumer
/// dinler (RET ise skor dusurme katsayisi gunceller).
/// </summary>
public sealed class RespondToOfferCommandHandler : IRequestHandler<RespondToOfferCommand, OfferDto>
{
    private readonly IOfferRepository _offerRepository;
    private readonly IOfferRatingRepository _offerRatingRepository;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public RespondToOfferCommandHandler(
        IOfferRepository offerRepository,
        IOfferRatingRepository offerRatingRepository,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _offerRepository = offerRepository;
        _offerRatingRepository = offerRatingRepository;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OfferDto> Handle(RespondToOfferCommand request, CancellationToken cancellationToken)
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

        if (offer.Status != OfferStatus.SUNULDU)
        {
            throw new ConflictException("OFFER_ALREADY_RESPONDED", "Bu teklife zaten yanit verilmis.");
        }

        var now = _dateTimeProvider.UtcNow;
        offer.Status = request.Response;
        offer.RespondedAt = now;

        await _publishEndpoint.PublishIntegrationEventAsync(
            new OfferRespondedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new OfferRespondedPayload(
                    offer.Id, offer.SubscriberId, offer.CampaignId, offer.Campaign.Type.ToString(), request.Response.ToString()),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var rating = await _offerRatingRepository.GetByOfferIdAsync(offer.Id, cancellationToken);
        return offer.ToDto(rating?.Stars);
    }
}
