using Gamification.Application.Common;
using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gamification.Application.Commands.ProcessCaseSlaBreached;

/// <summary>
/// case.sla_breached -&gt; -5 puan (Core_Principles §8). Rozet degerlendirmesi yok; bu event
/// yalnizca PointRuleEngine'in ileride gelecek campaign.optimized icin "hadPriorSlaBreach"
/// kontrolunu besler (HasSlaBreachForCaseAsync uzerinden).
/// </summary>
public sealed class ProcessCaseSlaBreachedCommandHandler : IRequestHandler<ProcessCaseSlaBreachedCommand>
{
    private readonly IExpertScoreRepository _expertScoreRepository;
    private readonly IPointTransactionRepository _pointTransactionRepository;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IGameNotifier _gameNotifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessCaseSlaBreachedCommandHandler> _logger;

    public ProcessCaseSlaBreachedCommandHandler(
        IExpertScoreRepository expertScoreRepository,
        IPointTransactionRepository pointTransactionRepository,
        ILeaderboardService leaderboardService,
        IGameNotifier gameNotifier,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ProcessCaseSlaBreachedCommandHandler> logger)
    {
        _expertScoreRepository = expertScoreRepository;
        _pointTransactionRepository = pointTransactionRepository;
        _leaderboardService = leaderboardService;
        _gameNotifier = gameNotifier;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessCaseSlaBreachedCommand request, CancellationToken cancellationToken)
    {
        if (await _pointTransactionRepository.ExistsByEventIdAsync(request.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "case.sla_breached zaten islenmis, atlaniyor (idempotency). EventId={EventId}", request.EventId);
            return;
        }

        var payload = request.Payload;

        if (payload.ExpertId is not { } expertId)
        {
            _logger.LogInformation(
                "case.sla_breached atanmamis vaka icin geldi, puan cezasi uygulanacak uzman yok. CaseId={CaseId}",
                payload.CaseId);
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        var expertScore = await _expertScoreRepository.GetByExpertIdAsync(expertId, cancellationToken);
        if (expertScore is null)
        {
            expertScore = new ExpertScore { ExpertId = expertId, DisplayName = string.Empty };
            _expertScoreRepository.Add(expertScore);
        }

        expertScore.TotalPoints += GamificationDefaults.SlaBreachPenalty;
        expertScore.UpdatedAt = now;

        _pointTransactionRepository.Add(new PointTransaction
        {
            ExpertId = expertId,
            EventId = request.EventId,
            Reason = PointReasons.SlaAsimi,
            Points = GamificationDefaults.SlaBreachPenalty,
            CaseId = payload.CaseId,
            CreatedAt = now,
        });

        await _leaderboardService.IncrementScoreAsync(expertId, GamificationDefaults.SlaBreachPenalty, now, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _gameNotifier.NotifyPointsUpdatedAsync(
            expertId, GamificationDefaults.SlaBreachPenalty, expertScore.TotalPoints, cancellationToken);
    }
}
