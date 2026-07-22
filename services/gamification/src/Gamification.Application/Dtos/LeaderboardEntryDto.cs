using Gamification.Domain.Enums;

namespace Gamification.Application.Dtos;

/// <summary>REST JSON camelCase (Core_Principles §4) — Api katmanında System.Text.Json varsayılan
/// camelCase policy'siyle serileşir; Level enum'u JsonStringEnumConverter ile UPPER_SNAKE yazılır.</summary>
public sealed record LeaderboardEntryDto(
    int Rank,
    Guid ExpertId,
    string DisplayName,
    int Points,
    ExpertLevel Level);
