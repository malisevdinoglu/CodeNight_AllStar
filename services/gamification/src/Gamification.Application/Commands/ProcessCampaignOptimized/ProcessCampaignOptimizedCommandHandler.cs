using Gamification.Application.Common;
using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using Gamification.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gamification.Application.Commands.ProcessCampaignOptimized;

/// <summary>
/// campaign.optimized -&gt; puanlama (PointRuleEngine) + sayaç güncellemeleri + rozet
/// değerlendirmesi (BadgeEvaluator) + leaderboard güncellemesi + SignalR bildirimi.
/// Yalnızca case TAMAMLANDI durumuna geçtiğinde yayınlanır (bkz. Campaign.Application
/// ChangeCaseStatusCommandHandler) — bu yüzden burada ayrıca bir durum kontrolü yapılmaz.
/// </summary>
public sealed class ProcessCampaignOptimizedCommandHandler : IRequestHandler<ProcessCampaignOptimizedCommand>
{
    private readonly IExpertScoreRepository _expertScoreRepository;
    private readonly IPointTransactionRepository _pointTransactionRepository;
    private readonly ISegmentCompletionCountRepository _segmentCompletionCountRepository;
    private readonly IExpertBadgeRepository _expertBadgeRepository;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IGameNotifier _gameNotifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessCampaignOptimizedCommandHandler> _logger;

    public ProcessCampaignOptimizedCommandHandler(
        IExpertScoreRepository expertScoreRepository,
        IPointTransactionRepository pointTransactionRepository,
        ISegmentCompletionCountRepository segmentCompletionCountRepository,
        IExpertBadgeRepository expertBadgeRepository,
        ILeaderboardService leaderboardService,
        IGameNotifier gameNotifier,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ProcessCampaignOptimizedCommandHandler> logger)
    {
        _expertScoreRepository = expertScoreRepository;
        _pointTransactionRepository = pointTransactionRepository;
        _segmentCompletionCountRepository = segmentCompletionCountRepository;
        _expertBadgeRepository = expertBadgeRepository;
        _leaderboardService = leaderboardService;
        _gameNotifier = gameNotifier;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessCampaignOptimizedCommand request, CancellationToken cancellationToken)
    {
        if (await _pointTransactionRepository.ExistsByEventIdAsync(request.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "campaign.optimized zaten islenmis, atlaniyor (idempotency). EventId={EventId}", request.EventId);
            return;
        }

        var payload = request.Payload;

        // Graceful degradation (Core_Principles §3): atanmamis uzmanli bir TAMAMLANDI event'i
        // sozlesme disi bir durumdur ama servis bu yuzden patlamamali - sadece atlanir.
        if (payload.ExpertId == Guid.Empty)
        {
            _logger.LogWarning(
                "campaign.optimized bos ExpertId ile geldi, puanlama atlaniyor. CaseId={CaseId}", payload.CaseId);
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        var hadPriorSlaBreach = await _pointTransactionRepository.HasSlaBreachForCaseAsync(payload.CaseId, cancellationToken);
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            payload.Priority, payload.ConversionLift, payload.CreatedAt, payload.CompletedAt, hadPriorSlaBreach);

        var expertScore = await _expertScoreRepository.GetByExpertIdAsync(payload.ExpertId, cancellationToken);
        if (expertScore is null)
        {
            expertScore = new ExpertScore { ExpertId = payload.ExpertId, DisplayName = string.Empty };
            _expertScoreRepository.Add(expertScore);
        }

        expertScore.TotalPoints += result.TotalPoints;
        expertScore.CompletedCaseCount += 1;
        if (result.FastCompletion)
        {
            expertScore.FastCompletionCount += 1;
        }

        if (result.TargetExceeded)
        {
            expertScore.TargetExceededCount += 1;
        }

        var isRiskliKayipSaved = string.Equals(payload.Segment, "RISKLI_KAYIP", StringComparison.Ordinal);
        if (isRiskliKayipSaved)
        {
            expertScore.RiskliKayipSavedCount += 1;
        }

        expertScore.UpdatedAt = now;

        var segmentCount = await _segmentCompletionCountRepository.GetAsync(payload.ExpertId, payload.Segment, cancellationToken);
        if (segmentCount is null)
        {
            segmentCount = new SegmentCompletionCount { ExpertId = payload.ExpertId, Segment = payload.Segment, CompletedCount = 0 };
            _segmentCompletionCountRepository.Add(segmentCount);
        }

        segmentCount.CompletedCount += 1;

        _pointTransactionRepository.Add(new PointTransaction
        {
            ExpertId = payload.ExpertId,
            EventId = request.EventId,
            Reason = PointReasons.OptimizasyonTamamlandi,
            Points = result.TotalPoints,
            CaseId = payload.CaseId,
            CreatedAt = now,
        });

        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var todayCompletedCount = await _pointTransactionRepository.CountTodayCompletionsAsync(
            payload.ExpertId, dayStart, dayStart.AddDays(1), cancellationToken);

        var alreadyEarned = await _expertBadgeRepository.GetEarnedBadgeCodesAsync(payload.ExpertId, cancellationToken);
        var newlyEarned = BadgeEvaluator.EvaluateNewlyEarned(new BadgeEvaluationContext(
            expertScore.CompletedCaseCount,
            expertScore.FastCompletionCount,
            expertScore.TargetExceededCount,
            expertScore.RiskliKayipSavedCount,
            todayCompletedCount,
            segmentCount.CompletedCount,
            alreadyEarned));

        foreach (var badgeCode in newlyEarned)
        {
            _expertBadgeRepository.Add(new ExpertBadge { ExpertId = payload.ExpertId, BadgeCode = badgeCode, EarnedAt = now });
        }

        await _leaderboardService.IncrementScoreAsync(payload.ExpertId, result.TotalPoints, now, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _gameNotifier.NotifyPointsUpdatedAsync(
            payload.ExpertId, result.TotalPoints, expertScore.TotalPoints, cancellationToken);

        foreach (var badgeCode in newlyEarned)
        {
            await _gameNotifier.NotifyBadgeEarnedAsync(payload.ExpertId, badgeCode, cancellationToken);
        }
    }
}
