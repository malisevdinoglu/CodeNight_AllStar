namespace Gamification.Application.Common;

/// <summary>Mali.md §7: Redis sorted set — haftalık (ISO 8601 hafta) ve tüm-zamanlar sıralaması.</summary>
public enum LeaderboardPeriod
{
    Weekly,
    AllTime
}

/// <summary>
/// Saf sıra verisi (ExpertId/Points/Rank) — DisplayName Redis'te TUTULMAZ; çağıran taraf
/// (query handler) <see cref="IExpertScoreRepository"/> ile Postgres'ten zenginleştirir.
/// </summary>
public sealed record LeaderboardEntry(Guid ExpertId, int Points, int Rank);

/// <summary>
/// StackExchange.Redis tabanlı implementasyon Infrastructure katmanında (ZINCRBY/ZREVRANGE/ZREVRANK).
/// Haftalık anahtar ISO 8601 hafta numarasıyla türetilir (ör. "leaderboard:weekly:2026-W30"),
/// tüm-zamanlar anahtarı sabittir (ör. "leaderboard:alltime").
/// </summary>
public interface ILeaderboardService
{
    /// <summary>Hem haftalık hem tüm-zamanlar sorted set'lerini tek çağrıda günceller.</summary>
    Task IncrementScoreAsync(
        Guid expertId, int pointsDelta, DateTimeOffset occurredAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(
        LeaderboardPeriod period, int count, CancellationToken cancellationToken = default);

    Task<int?> GetRankAsync(
        Guid expertId, LeaderboardPeriod period, CancellationToken cancellationToken = default);
}
