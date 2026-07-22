using Gamification.Application.Common;
using Gamification.Application.Dtos;
using Gamification.Application.External;
using Gamification.Domain.Services;
using MediatR;

namespace Gamification.Application.Queries.GetLeaderboard;

/// <summary>
/// Sıra/puan Redis'ten (ILeaderboardService — kaynak-doğru, ZREVRANGE), görünen ad
/// Identity'den (cold path), seviye Postgres'teki TÜM-ZAMANLAR TotalPoints'ten hesaplanır
/// (haftalık görünümde bile seviye her zaman toplam puana göredir — Mali.md §7).
/// </summary>
public sealed class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, IReadOnlyList<LeaderboardEntryDto>>
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IExpertScoreRepository _expertScoreRepository;
    private readonly IIdentityServiceClient _identityServiceClient;

    public GetLeaderboardQueryHandler(
        ILeaderboardService leaderboardService,
        IExpertScoreRepository expertScoreRepository,
        IIdentityServiceClient identityServiceClient)
    {
        _leaderboardService = leaderboardService;
        _expertScoreRepository = expertScoreRepository;
        _identityServiceClient = identityServiceClient;
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> Handle(
        GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var topEntries = await _leaderboardService.GetTopAsync(request.Period, request.Count, cancellationToken);
        if (topEntries.Count == 0)
        {
            return Array.Empty<LeaderboardEntryDto>();
        }

        var expertIds = topEntries.Select(e => e.ExpertId).ToList();
        var scoresById = await _expertScoreRepository.GetManyByIdsAsync(expertIds, cancellationToken);

        // Identity erisilemezse (graceful degradation) isimler cache'lenmis ExpertScore.DisplayName'e
        // veya "Bilinmeyen"e duser - liderlik tablosu asla bu yuzden 500 dondurmez.
        IReadOnlyDictionary<Guid, IdentityUserDto> identityById;
        try
        {
            identityById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(u => u.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            identityById = new Dictionary<Guid, IdentityUserDto>();
        }

        return topEntries
            .Select(entry =>
            {
                scoresById.TryGetValue(entry.ExpertId, out var score);
                var displayName = ResolveDisplayName(entry.ExpertId, identityById, score?.DisplayName);
                var level = LevelCalculator.Calculate(score?.TotalPoints ?? entry.Points);

                return new LeaderboardEntryDto(entry.Rank, entry.ExpertId, displayName, entry.Points, level);
            })
            .ToList();
    }

    private static string ResolveDisplayName(
        Guid expertId, IReadOnlyDictionary<Guid, IdentityUserDto> identityById, string? cachedDisplayName)
    {
        if (identityById.TryGetValue(expertId, out var identityUser))
        {
            return $"{identityUser.FirstName} {identityUser.LastName}";
        }

        return string.IsNullOrWhiteSpace(cachedDisplayName) ? "Bilinmeyen" : cachedDisplayName;
    }
}
