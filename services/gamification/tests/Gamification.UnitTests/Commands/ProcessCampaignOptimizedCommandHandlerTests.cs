using Gamification.Application.Commands.ProcessCampaignOptimized;
using Gamification.Application.Common;
using Gamification.Application.Events;
using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Gamification.UnitTests.Commands;

/// <summary>
/// point_transactions.event_id UNIQUE (İskender.md §3) idempotency senaryosu + normal akışta
/// puan/sayaç/rozet/leaderboard/SignalR zincirinin uçtan uca (mock repository'lerle) doğrulanması.
/// </summary>
public sealed class ProcessCampaignOptimizedCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 15, 0, 0, TimeSpan.Zero);

    private readonly Mock<IExpertScoreRepository> _expertScoreRepository = new();
    private readonly Mock<IPointTransactionRepository> _pointTransactionRepository = new();
    private readonly Mock<ISegmentCompletionCountRepository> _segmentCompletionCountRepository = new();
    private readonly Mock<IExpertBadgeRepository> _expertBadgeRepository = new();
    private readonly Mock<ILeaderboardService> _leaderboardService = new();
    private readonly Mock<IGameNotifier> _gameNotifier = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ProcessCampaignOptimizedCommandHandler>> _logger = new();

    private readonly ProcessCampaignOptimizedCommandHandler _handler;

    public ProcessCampaignOptimizedCommandHandlerTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _pointTransactionRepository
            .Setup(r => r.HasSlaBreachForCaseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _pointTransactionRepository
            .Setup(r => r.CountTodayCompletionsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _expertBadgeRepository
            .Setup(r => r.GetEarnedBadgeCodesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        _handler = new ProcessCampaignOptimizedCommandHandler(
            _expertScoreRepository.Object,
            _pointTransactionRepository.Object,
            _segmentCompletionCountRepository.Object,
            _expertBadgeRepository.Object,
            _leaderboardService.Object,
            _gameNotifier.Object,
            _dateTimeProvider.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private static CampaignOptimizedPayload MakePayload(
        Guid expertId, string segment = "YUKSEK_DEGER", string priority = "ORTA",
        decimal? conversionLift = 2m, DateTimeOffset? createdAt = null, DateTimeOffset? completedAt = null) =>
        new(
            CaseId: Guid.NewGuid(),
            ExpertId: expertId,
            Segment: segment,
            Priority: priority,
            ConversionLift: conversionLift,
            CreatedAt: createdAt ?? Now.AddHours(-3),
            CompletedAt: completedAt ?? Now);

    [Fact]
    public async Task Ayni_event_ikinci_kez_gelirse_hicbir_sey_yapilmadan_atlanir()
    {
        var eventId = Guid.NewGuid();
        _pointTransactionRepository
            .Setup(r => r.ExistsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ProcessCampaignOptimizedCommand(eventId, MakePayload(Guid.NewGuid()));

        await _handler.Handle(command, CancellationToken.None);

        _expertScoreRepository.Verify(
            r => r.GetByExpertIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _pointTransactionRepository.Verify(r => r.Add(It.IsAny<PointTransaction>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Bos_ExpertId_gelirse_graceful_sekilde_atlanir_hata_firlatmaz()
    {
        _pointTransactionRepository
            .Setup(r => r.ExistsByEventIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ProcessCampaignOptimizedCommand(Guid.NewGuid(), MakePayload(Guid.Empty));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _pointTransactionRepository.Verify(r => r.Add(It.IsAny<PointTransaction>()), Times.Never);
    }

    [Fact]
    public async Task Yeni_uzman_icin_puan_sayac_rozet_leaderboard_ve_bildirim_zinciri_dogru_calisir()
    {
        var expertId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        _pointTransactionRepository
            .Setup(r => r.ExistsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _expertScoreRepository
            .Setup(r => r.GetByExpertIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpertScore?)null);
        _segmentCompletionCountRepository
            .Setup(r => r.GetAsync(expertId, "RISKLI_KAYIP", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SegmentCompletionCount?)null);

        // Hiz bonusu (30 dk < 2s) + KRITIK&SLA-ici bonusu (hic ihlal yok) tetiklenir, hedef asimi YOK.
        // 10 (taban) + 5 (hiz) + 15 (kritik) = 30.
        var payload = MakePayload(
            expertId,
            segment: "RISKLI_KAYIP",
            priority: "KRITIK",
            conversionLift: 1m,
            createdAt: Now.AddMinutes(-30),
            completedAt: Now);

        var command = new ProcessCampaignOptimizedCommand(eventId, payload);

        await _handler.Handle(command, CancellationToken.None);

        _expertScoreRepository.Verify(r => r.Add(It.Is<ExpertScore>(s =>
            s.ExpertId == expertId
            && s.TotalPoints == 30
            && s.CompletedCaseCount == 1
            && s.FastCompletionCount == 1
            && s.TargetExceededCount == 0
            && s.RiskliKayipSavedCount == 1)), Times.Once);

        _pointTransactionRepository.Verify(r => r.Add(It.Is<PointTransaction>(t =>
            t.ExpertId == expertId
            && t.EventId == eventId
            && t.Points == 30
            && t.Reason == PointReasons.OptimizasyonTamamlandi
            && t.CaseId == payload.CaseId)), Times.Once);

        _segmentCompletionCountRepository.Verify(
            r => r.Add(It.Is<SegmentCompletionCount>(c => c.ExpertId == expertId && c.Segment == "RISKLI_KAYIP")),
            Times.Once);

        // Sadece 1 tamamlanan vaka var -> ILK_KAMPANYA disinda hicbir esik karsilanmaz.
        _expertBadgeRepository.Verify(
            r => r.Add(It.Is<ExpertBadge>(b => b.ExpertId == expertId && b.BadgeCode == BadgeCodes.IlkKampanya)),
            Times.Once);
        _gameNotifier.Verify(
            n => n.NotifyBadgeEarnedAsync(expertId, BadgeCodes.IlkKampanya, It.IsAny<CancellationToken>()),
            Times.Once);

        _leaderboardService.Verify(
            l => l.IncrementScoreAsync(expertId, 30, Now, It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _gameNotifier.Verify(
            n => n.NotifyPointsUpdatedAsync(expertId, 30, 30, It.IsAny<CancellationToken>()), Times.Once);
    }
}
