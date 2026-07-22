using BuildingBlocks.Exceptions;
using Gamification.Application.Common;
using Gamification.Application.Dtos;
using Gamification.Application.External;
using Gamification.Domain.Services;
using MediatR;

namespace Gamification.Application.Queries.GetProfile;

/// <summary>
/// Henüz hiç puan hareketi olmayan (yeni) bir uzman için ExpertScore satırı yoktur — bu 404
/// DEĞİLDİR, sıfır durumlu bir profildir (Core_Principles §3 graceful degradation). 404 SADECE
/// Identity'de böyle bir uzman hiç yoksa döner.
/// </summary>
public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDto>
{
    private readonly IExpertScoreRepository _expertScoreRepository;
    private readonly IExpertBadgeRepository _expertBadgeRepository;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IIdentityServiceClient _identityServiceClient;

    public GetProfileQueryHandler(
        IExpertScoreRepository expertScoreRepository,
        IExpertBadgeRepository expertBadgeRepository,
        ILeaderboardService leaderboardService,
        IIdentityServiceClient identityServiceClient)
    {
        _expertScoreRepository = expertScoreRepository;
        _expertBadgeRepository = expertBadgeRepository;
        _leaderboardService = leaderboardService;
        _identityServiceClient = identityServiceClient;
    }

    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        string displayName;
        try
        {
            var experts = await _identityServiceClient.GetExpertsAsync(cancellationToken);
            var identityUser = experts.FirstOrDefault(u => u.Id == request.ExpertId)
                ?? throw new NotFoundException("EXPERT_NOT_FOUND", "Uzman bulunamadi.");
            displayName = $"{identityUser.FirstName} {identityUser.LastName}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not NotFoundException)
        {
            // Identity erisilemez durumdaysa profili tumden reddetmek yerine bilinen (cache'lenmis)
            // isimle devam edilir; hic veri yoksa "Bilinmeyen" ile graceful degrade edilir.
            displayName = "Bilinmeyen";
        }

        var expertScore = await _expertScoreRepository.GetByExpertIdAsync(request.ExpertId, cancellationToken);
        var badges = await _expertBadgeRepository.GetByExpertIdWithBadgeAsync(request.ExpertId, cancellationToken);
        var weeklyRank = await _leaderboardService.GetRankAsync(request.ExpertId, LeaderboardPeriod.Weekly, cancellationToken);
        var allTimeRank = await _leaderboardService.GetRankAsync(request.ExpertId, LeaderboardPeriod.AllTime, cancellationToken);

        if (!string.IsNullOrWhiteSpace(expertScore?.DisplayName) && displayName == "Bilinmeyen")
        {
            displayName = expertScore.DisplayName;
        }

        return new ProfileDto(
            request.ExpertId,
            displayName,
            expertScore?.TotalPoints ?? 0,
            LevelCalculator.Calculate(expertScore?.TotalPoints ?? 0),
            expertScore?.CompletedCaseCount ?? 0,
            expertScore?.FastCompletionCount ?? 0,
            expertScore?.TargetExceededCount ?? 0,
            expertScore?.RiskliKayipSavedCount ?? 0,
            weeklyRank,
            allTimeRank,
            badges.Select(b => new BadgeDto(b.BadgeCode, b.Badge.Name, b.Badge.Description, b.EarnedAt)).ToList());
    }
}
