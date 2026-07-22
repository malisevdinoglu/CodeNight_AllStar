using Gamification.Domain.Enums;

namespace Gamification.Application.Dtos;

public sealed record ProfileDto(
    Guid ExpertId,
    string DisplayName,
    int TotalPoints,
    ExpertLevel Level,
    int CompletedCaseCount,
    int FastCompletionCount,
    int TargetExceededCount,
    int RiskliKayipSavedCount,
    int? WeeklyRank,
    int? AllTimeRank,
    IReadOnlyList<BadgeDto> Badges);
