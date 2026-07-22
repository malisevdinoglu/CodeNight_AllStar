using Campaign.Application.Commands.AssignExpert;
using Campaign.Application.Commands.CreateCampaign;
using Campaign.Application.Common;
using Campaign.Application.Events;
using Campaign.Application.External;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using FluentAssertions;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.UnitTests.Commands;

/// <summary>
/// Mali.md "demo adım 7 sigortası": AI /recommend çağrısı null dönerse (kapalı/timeout)
/// kampanya YİNE oluşur, segment BELIRSIZ + öncelik ORTA + teklif YOK olarak. Bu, akışın hiçbir
/// koşulda kesilmemesi gereken en kritik dalıdır — bu yüzden ayrı bir handler testiyle kapatılır.
/// NOT: Bu proje bu sandbox'ta derlenemedi (.NET SDK yok) — `dotnet test` ile doğrulanmalı.
/// </summary>
public sealed class CreateCampaignCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICampaignRepository> _campaignRepository = new();
    private readonly Mock<IOptimizationCaseRepository> _caseRepository = new();
    private readonly Mock<IOfferRepository> _offerRepository = new();
    private readonly Mock<ISubscriberProfileRepository> _subscriberProfileRepository = new();
    private readonly Mock<INumberSequenceProvider> _numberSequence = new();
    private readonly Mock<IAiServiceClient> _aiServiceClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<CreateCampaignCommandHandler>> _logger = new();

    private readonly Guid _supervizorId = Guid.NewGuid();
    private readonly CreateCampaignCommandHandler _handler;

    public CreateCampaignCommandHandlerTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
        _requestContext.Setup(c => c.UserId).Returns(_supervizorId);

        _numberSequence.Setup(s => s.NextCampaignNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("CMP-2026-000001");
        _numberSequence.Setup(s => s.NextCaseNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OPT-2026-000001");

        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _publishEndpoint
            .Setup(p => p.Publish(
                It.IsAny<CampaignCreatedEvent>(),
                It.IsAny<IPipe<PublishContext<CampaignCreatedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publishEndpoint
            .Setup(p => p.Publish(
                It.IsAny<CaseCreatedEvent>(),
                It.IsAny<IPipe<PublishContext<CaseCreatedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediator
            .Setup(m => m.Send(It.IsAny<AssignExpertCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new CreateCampaignCommandHandler(
            _campaignRepository.Object,
            _caseRepository.Object,
            _offerRepository.Object,
            _subscriberProfileRepository.Object,
            _numberSequence.Object,
            _aiServiceClient.Object,
            _unitOfWork.Object,
            _publishEndpoint.Object,
            _requestContext.Object,
            _dateTimeProvider.Object,
            _mediator.Object,
            _logger.Object);
    }

    private static SubscriberProfile MakeSubscriber() => new()
    {
        SubscriberId = Guid.NewGuid(),
        GsmNumber = "5550001122",
        CurrentPlan = "Standart",
        TenureMonths = 12,
        AvgMonthlyDataGb = 10m,
        AvgMonthlyCallMinutes = 300,
        MonthlySpendTl = 150m,
        PackagePurchaseCount = 1,
        ComplaintCount = 0,
        DaysSinceLastActivity = 2,
        PastAcceptanceRate = 0.30m,
    };

    [Fact]
    public async Task AI_null_donerse_kampanya_yine_olusur_BELIRSIZ_ORTA_ile_teklifsiz()
    {
        var subscribers = new[] { MakeSubscriber(), MakeSubscriber() };
        _subscriberProfileRepository
            .Setup(r => r.GetBySegmentAsync(SegmentType.YUKSEK_DEGER, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscribers);
        _aiServiceClient
            .Setup(c => c.RecommendAsync(It.IsAny<AiRecommendRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AiRecommendationDto>?)null);

        var command = new CreateCampaignCommand("Yaz Kampanyasi", CampaignType.EK_PAKET, SegmentType.YUKSEK_DEGER, "aciklama");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.PredictedSegment.Should().Be(SegmentType.BELIRSIZ);
        result.Priority.Should().Be(CasePriority.ORTA);
        result.ConversionProbability.Should().BeNull();
        result.AiAvailable.Should().BeFalse();
        result.CampaignNumber.Should().Be("CMP-2026-000001");
        result.CaseNumber.Should().Be("OPT-2026-000001");

        _offerRepository.Verify(r => r.Add(It.IsAny<Offer>()), Times.Never,
            "AI kapaliyken hicbir teklif olusturulmamali");
        _campaignRepository.Verify(r => r.Add(It.IsAny<CampaignEntity>()), Times.Once,
            "AI kapali olsa bile kampanya YINE olusturulmali (demo adim 7 sigortasi)");
        _caseRepository.Verify(r => r.Add(It.IsAny<OptimizationCase>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AI_basariliysa_esik_alti_skorlar_teklife_donusmez_uzeri_donusur()
    {
        var lowScoreSubscriber = MakeSubscriber();
        var highScoreSubscriber = MakeSubscriber();
        var subscribers = new[] { lowScoreSubscriber, highScoreSubscriber };

        _subscriberProfileRepository
            .Setup(r => r.GetBySegmentAsync(SegmentType.YUKSEK_DEGER, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscribers);

        // CampaignId handler icinde Guid.NewGuid() ile uretildigi icin onceden bilinemez;
        // istekten (Campaigns[0].CampaignId) okuyup ayni degeri geri dondurerek eslestiriyoruz.
        _aiServiceClient
            .Setup(c => c.RecommendAsync(It.IsAny<AiRecommendRequest>(), It.IsAny<CancellationToken>()))
            .Returns<AiRecommendRequest, CancellationToken>((req, _) =>
            {
                var campaignId = req.Campaigns[0].CampaignId;
                var isHighScore = req.SubscriberProfile.SubscriberId == highScoreSubscriber.SubscriberId;
                var score = isHighScore ? 0.85m : 0.40m;
                IReadOnlyList<AiRecommendationDto>? recommendations = new List<AiRecommendationDto>
                {
                    new(campaignId, score, score - 0.05m)
                };
                return Task.FromResult(recommendations);
            });

        var command = new CreateCampaignCommand("Yaz Kampanyasi", CampaignType.EK_PAKET, SegmentType.YUKSEK_DEGER, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AiAvailable.Should().BeTrue();
        result.PredictedSegment.Should().Be(SegmentType.YUKSEK_DEGER);

        _offerRepository.Verify(
            r => r.Add(It.Is<Offer>(o => o.SubscriberId == highScoreSubscriber.SubscriberId)), Times.Once);
        _offerRepository.Verify(
            r => r.Add(It.Is<Offer>(o => o.SubscriberId == lowScoreSubscriber.SubscriberId)), Times.Never,
            "skor 0.60 altinda teklif olusturulmamali");
        _offerRepository.Verify(
            r => r.Add(It.Is<Offer>(o => o.IsPriority)), Times.Once,
            "skor 0.80 uzeri isPriority=true olmali");
    }
}
