namespace Gamification.Application.Common;

/// <summary>
/// badge.earned / points.updated Core_Principles §8 event kataloğunda RabbitMQ üzerinden değil,
/// doğrudan SignalR push olarak listelenir ("(SignalR push)" notasyonu) — bu yüzden burada
/// IntegrationEvent değil, ince bir bildirim arayüzü var. Api katmanı GameHub (IHubContext) ile
/// implemente eder; Application katmanı SignalR'a doğrudan bağımlı olmaz.
/// </summary>
public interface IGameNotifier
{
    Task NotifyPointsUpdatedAsync(
        Guid expertId, int pointsDelta, int totalPoints, CancellationToken cancellationToken = default);

    Task NotifyBadgeEarnedAsync(
        Guid expertId, string badgeCode, CancellationToken cancellationToken = default);
}
