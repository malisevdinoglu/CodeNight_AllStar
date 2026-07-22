using Gamification.Application.Common;
using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Gamification.Application.Commands.ProcessOfferRated;

/// <summary>
/// offer.rated -&gt; SADECE 1-2 yildiz icin -3 puan (Core_Principles §8). 3-5 yildiz notr'dur,
/// hicbir puan hareketi yaratilmaz (islem yok = tekrar teslimat da zararsizdir, ekstra bir
/// idempotency kaydi gerekmez).
/// </summary>
public sealed class ProcessOfferRatedCommandHandler : IRequestHandler<ProcessOfferRatedCommand>
{
    private const int LowRatingMaxStars = 2;

    private readonly IExpertScoreRepository _expertScoreRepository;
    private readonly IPointTransactionRepository _pointTransactionRepository;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IGameNotifier _gameNotifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessOfferRatedCommandHandler> _logger;

    public ProcessOfferRatedCommandHandler(
        IExpertScoreRepository expertScoreRepository,
        IPointTransactionRepository pointTransactionRepository,
        ILeaderboardService leaderboardService,
        IGameNotifier gameNotifier,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ProcessOfferRatedCommandHandler> logger)
    {
        _expertScoreRepository = expertScoreRepository;
        _pointTransactionRepository = pointTransactionRepository;
        _leaderboardService = leaderboardService;
        _gameNotifier = gameNotifier;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ProcessOfferRatedCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;

        if (payload.Stars > LowRatingMaxStars)
        {
            return;
        }

        if (await _pointTransactionRepository.ExistsByEventIdAsync(request.EventId, cancellationToken))
        {
            _logger.LogInformation(
                "offer.rated zaten islenmis, atlaniyor (idempotency). EventId={EventId}", request.EventId);
            return;
        }

        if (payload.ExpertId is not { } expertId)
        {
            _logger.LogInformation(
                "offer.rated dusuk puanli ama atanmis uzman yok, ceza uygulanamiyor. OfferId={OfferId}",
                payload.OfferId);
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        var expertScore = await _expertScoreRepository.GetByExpertIdAsync(expertId, cancellationToken);
        if (expertScore is null)
        {
            expertScore = new ExpertScore { ExpertId = expertId, DisplayName = string.Empty };
            _expertScoreRepository.Add(expertScore);
        }

        expertScore.TotalPoints += GamificationDefaults.LowRatingPenalty;
        expertScore.UpdatedAt = now;

        _pointTransactionRepository.Add(new PointTransaction
        {
            ExpertId = expertId,
            EventId = request.EventId,
            Reason = PointReasons.DusukPuan,
            Points = GamificationDefaults.LowRatingPenalty,
            CaseId = null,
            CreatedAt = now,
        });

        await _leaderboardService.IncrementScoreAsync(expertId, GamificationDefaults.LowRatingPenalty, now, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _gameNotifier.NotifyPointsUpdatedAsync(
            expertId, GamificationDefaults.LowRatingPenalty, expertScore.TotalPoints, cancellationToken);
    }
}
