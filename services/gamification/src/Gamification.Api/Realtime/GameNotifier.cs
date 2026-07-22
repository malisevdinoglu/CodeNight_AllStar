using BuildingBlocks.Events;
using Gamification.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace Gamification.Api.Realtime;

/// <summary>
/// IGameNotifier'ın Api implementasyonu (Application katmanı SignalR'a bağımlı değil).
/// SignalR metod adları, Core_Principles §8 event kataloğundaki event_type değerleriyle
/// BİREBİR aynı ("points.updated"/"badge.earned") - frontend (Osman) tek bir sözlükten
/// dinler, REST/SignalR arasında isim farkı olmaz.
/// </summary>
public sealed class GameNotifier : IGameNotifier
{
    private readonly IHubContext<GameHub> _hubContext;

    public GameNotifier(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyPointsUpdatedAsync(
        Guid expertId, int pointsDelta, int totalPoints, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.User(expertId.ToString()).SendAsync(
            EventTypes.PointsUpdated,
            new PointsUpdatedPayload(expertId, pointsDelta, totalPoints),
            cancellationToken);

    public Task NotifyBadgeEarnedAsync(
        Guid expertId, string badgeCode, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.User(expertId.ToString()).SendAsync(
            EventTypes.BadgeEarned,
            new BadgeEarnedPayload(expertId, badgeCode),
            cancellationToken);

    private sealed record PointsUpdatedPayload(Guid ExpertId, int PointsDelta, int TotalPoints);

    private sealed record BadgeEarnedPayload(Guid ExpertId, string BadgeCode);
}
