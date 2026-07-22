using BuildingBlocks.Exceptions;
using BuildingBlocks.Messaging;
using Campaign.Application.Commands.AssignExpert;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.Events;
using Campaign.Application.External;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Application.Commands.CreateCampaign;

/// <summary>
/// Mali_Plan.md kritik akış: kampanya kaydı → hedef segment abone havuzu → AI /recommend
/// (3 sn timeout) → skor ≥ 0.60 teklif (skor &gt; 0.80 isPriority) → case aç → event yayınla
/// → düşük dönüşümlü case için uzman atamasını tetikle.
/// AI çökerse (null dönerse) kampanya YİNE oluşur: segment BELIRSIZ, öncelik ORTA, teklif YOK,
/// case manuel kuyruğa düşer (Core_Principles §3 graceful degradation — "demo adım 7 sigortası").
/// </summary>
public sealed class CreateCampaignCommandHandler : IRequestHandler<CreateCampaignCommand, CreateCampaignResult>
{
    private readonly ICampaignRepository _campaignRepository;
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly ISubscriberProfileRepository _subscriberProfileRepository;
    private readonly INumberSequenceProvider _numberSequence;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateCampaignCommandHandler> _logger;

    public CreateCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        ISubscriberProfileRepository subscriberProfileRepository,
        INumberSequenceProvider numberSequence,
        IAiServiceClient aiServiceClient,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IMediator mediator,
        ILogger<CreateCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _subscriberProfileRepository = subscriberProfileRepository;
        _numberSequence = numberSequence;
        _aiServiceClient = aiServiceClient;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<CreateCampaignResult> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        if (_requestContext.UserId is not { } createdBy)
        {
            throw new ForbiddenException("CMP_403_UNAUTHENTICATED", "Kimlik dogrulanamadi.");
        }

        var now = _dateTimeProvider.UtcNow;
        var validFrom = DateOnly.FromDateTime(now.UtcDateTime);

        // 1) Kampanya kaydi (numara DB sequence'inden - Iskender.md §2 campaign_number_seq)
        var campaign = new CampaignEntity
        {
            Id = Guid.NewGuid(),
            CampaignNumber = await _numberSequence.NextCampaignNumberAsync(cancellationToken),
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            TargetSegment = request.TargetSegment,
            DiscountRate = CampaignDefaults.GetDefaultDiscountRate(request.Type),
            ValidFrom = validFrom,
            ValidUntil = CampaignDefaults.GetDefaultValidUntil(validFrom),
            Status = CampaignStatus.AKTIF,
            CreatedBy = createdBy,
            CreatedAt = now,
        };
        _campaignRepository.Add(campaign);

        // 2) Hedef segmentteki abone havuzu (kendi DB'si)
        var subscribers = await _subscriberProfileRepository.GetBySegmentAsync(request.TargetSegment, cancellationToken);

        // 3) AI cagrisi (basarisizlikta IAiServiceClient null doner, exception FIRLATMAZ)
        var (offers, predictedSegment, priority, avgConversionProbability, aiAvailable) =
            await TryBuildOffersViaAiAsync(campaign, subscribers, request.TargetSegment, now, cancellationToken);

        foreach (var offer in offers)
        {
            _offerRepository.Add(offer);
        }

        var optimizationCase = new OptimizationCase
        {
            Id = Guid.NewGuid(),
            CaseNumber = await _numberSequence.NextCaseNumberAsync(cancellationToken),
            CampaignId = campaign.Id,
            Segment = predictedSegment,
            Priority = priority,
            Status = CaseStatus.YENI,
            SlaDeadline = SlaPolicy.CalculateDeadline(priority, now),
            CreatedAt = now,
        };
        _caseRepository.Add(optimizationCase);

        // 4) Event'leri yayinla (Outbox ile - MassTransit AddEntityFrameworkOutbox, Infrastructure DI'da yapilandirilir;
        // handler her zaman ayni sekilde IPublishEndpoint kullanir, outbox'in acik/kapali olmasindan bagimsizdir)
        await _publishEndpoint.PublishIntegrationEventAsync(
            new CampaignCreatedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new CampaignCreatedPayload(
                    campaign.Id, campaign.CampaignNumber, campaign.Type.ToString(),
                    campaign.TargetSegment.ToString(), createdBy),
            },
            cancellationToken);

        await _publishEndpoint.PublishIntegrationEventAsync(
            new CaseCreatedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new CaseCreatedPayload(
                    optimizationCase.Id, campaign.Id, optimizationCase.Segment.ToString(),
                    optimizationCase.Priority.ToString(), optimizationCase.SlaDeadline),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5) Vaka acildigina gore (dusuk donusum/optimizasyon gereken segment) uzman atamasini tetikle.
        // AI kapaliysa veya kapasite yoksa case YENI kalir - dashboard "bekleyen kuyruk"ta gosterir.
        try
        {
            await _mediator.Send(new AssignExpertCommand(optimizationCase.Id), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Otomatik uzman atamasi basarisiz oldu, vaka YENI/kuyrukta kaliyor. CaseId={CaseId}",
                optimizationCase.Id);
        }

        return new CreateCampaignResult(
            campaign.Id, campaign.CampaignNumber, optimizationCase.Id, optimizationCase.CaseNumber,
            predictedSegment, priority, avgConversionProbability, aiAvailable);
    }

    private async Task<(List<Offer> Offers, SegmentType Segment, CasePriority Priority, decimal? AvgConversionProbability, bool AiAvailable)>
        TryBuildOffersViaAiAsync(
            CampaignEntity campaign,
            IReadOnlyList<SubscriberProfile> subscribers,
            SegmentType targetSegment,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        var aiCampaigns = new[] { new AiCampaignSummaryDto(campaign.Id, campaign.Type, campaign.DiscountRate) };
        var offers = new List<Offer>();
        var conversionProbabilities = new List<decimal>();

        foreach (var subscriber in subscribers)
        {
            var request = new AiRecommendRequest(ToAiProfile(subscriber), aiCampaigns);

            IReadOnlyList<AiRecommendationDto>? recommendations;
            try
            {
                recommendations = await _aiServiceClient.RecommendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                recommendations = null;
                _logger.LogWarning(ex, "AI /recommend cagrisinda beklenmeyen hata. CampaignId={CampaignId}", campaign.Id);
            }

            if (recommendations is null)
            {
                // AI kapali: fallback'e gec. Simdiye kadar biriken teklifler iptal edilir (tum-ya-da-hic tutarlilik).
                _logger.LogWarning(
                    "AI servisi kullanilamiyor, kampanya BELIRSIZ/ORTA fallback ile devam ediyor. CampaignId={CampaignId}",
                    campaign.Id);
                return ([], SegmentType.BELIRSIZ, CasePriority.ORTA, null, false);
            }

            var recommendation = recommendations.FirstOrDefault(r => r.CampaignId == campaign.Id);
            if (recommendation is null || recommendation.RecommendationScore < 0.60m)
            {
                continue;
            }

            offers.Add(new Offer
            {
                Id = Guid.NewGuid(),
                CampaignId = campaign.Id,
                SubscriberId = subscriber.SubscriberId,
                RecommendationScore = recommendation.RecommendationScore,
                ConversionProbability = recommendation.ConversionProbability,
                IsPriority = recommendation.RecommendationScore > 0.80m,
                Status = OfferStatus.SUNULDU,
                CreatedAt = now,
            });
            conversionProbabilities.Add(recommendation.ConversionProbability);
        }

        var avgConversionProbability = conversionProbabilities.Count > 0
            ? conversionProbabilities.Average()
            : (decimal?)null;

        var priority = SlaPolicy.ApplyMinimumForSegment(
            CampaignDefaults.DeterminePriorityFromConversion(avgConversionProbability), targetSegment);

        return (offers, targetSegment, priority, avgConversionProbability, true);
    }

    private static AiSubscriberProfileDto ToAiProfile(SubscriberProfile subscriber) => new(
        subscriber.SubscriberId,
        subscriber.CurrentPlan,
        subscriber.TenureMonths,
        subscriber.AvgMonthlyDataGb,
        subscriber.AvgMonthlyCallMinutes,
        subscriber.MonthlySpendTl,
        subscriber.PackagePurchaseCount,
        subscriber.ComplaintCount,
        subscriber.DaysSinceLastActivity,
        subscriber.PastAcceptanceRate);
}
