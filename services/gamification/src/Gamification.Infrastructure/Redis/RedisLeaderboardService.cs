using System.Globalization;
using Gamification.Application.Common;
using StackExchange.Redis;

namespace Gamification.Infrastructure.Redis;

/// <summary>
/// Mali.md §7: Redis sorted set (ZINCRBY/ZREVRANGE/ZREVRANK). "Weekly" HER ZAMAN İÇİNDE
/// BULUNULAN ISO 8601 haftasını ifade eder — GetTopAsync/GetRankAsync çağrı anındaki haftayı
/// kullanır, IncrementScoreAsync ise event'in occurredAt'ına ait haftayı günceller (normal
/// akışta bu ikisi aynıdır; yalnızca gecikmeli/eski event işleme durumunda ayrışabilir, ki bu da
/// doğru davranıştır — puan, oluştuğu haftaya yazılmalıdır).
/// </summary>
public sealed class RedisLeaderboardService : ILeaderboardService
{
    private const string AllTimeKey = "leaderboard:alltime";

    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisLeaderboardService(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task IncrementScoreAsync(
        Guid expertId, int pointsDelta, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var member = expertId.ToString();

        await db.SortedSetIncrementAsync(AllTimeKey, member, pointsDelta);
        await db.SortedSetIncrementAsync(WeeklyKey(occurredAt), member, pointsDelta);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(
        LeaderboardPeriod period, int count, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var key = ResolveKey(period, DateTimeOffset.UtcNow);

        var entries = await db.SortedSetRangeByRankWithScoresAsync(key, 0, count - 1, Order.Descending);

        var result = new List<LeaderboardEntry>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            if (Guid.TryParse(entries[i].Element.ToString(), out var expertId))
            {
                result.Add(new LeaderboardEntry(expertId, (int)entries[i].Score, i + 1));
            }
        }

        return result;
    }

    public async Task<int?> GetRankAsync(
        Guid expertId, LeaderboardPeriod period, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var key = ResolveKey(period, DateTimeOffset.UtcNow);

        var rank = await db.SortedSetRankAsync(key, expertId.ToString(), Order.Descending);
        return rank is null ? null : (int)rank.Value + 1;
    }

    private static string ResolveKey(LeaderboardPeriod period, DateTimeOffset now) => period switch
    {
        LeaderboardPeriod.AllTime => AllTimeKey,
        LeaderboardPeriod.Weekly => WeeklyKey(now),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };

    private static string WeeklyKey(DateTimeOffset timestamp)
    {
        var date = timestamp.UtcDateTime;
        var isoYear = ISOWeek.GetYear(date);
        var isoWeek = ISOWeek.GetWeekOfYear(date);
        return $"leaderboard:weekly:{isoYear}-W{isoWeek:D2}";
    }
}
