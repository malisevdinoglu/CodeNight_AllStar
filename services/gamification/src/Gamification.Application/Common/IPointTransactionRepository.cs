using Gamification.Domain.Entities;

namespace Gamification.Application.Common;

/// <summary>
/// point_transactions.event_id UNIQUE (İskender.md §3) — idempotency BURADA, DB kısıtıyla
/// birlikte iki katmanlı garanti altına alınır: ExistsByEventIdAsync ön-kontrolü (RabbitMQ
/// retry'de gereksiz iş yapmamak için) + DB UNIQUE (yarış durumuna karşı son savunma hattı).
/// </summary>
public interface IPointTransactionRepository
{
    void Add(PointTransaction transaction);

    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// "KRITIK &amp; SLA içinde" bonusu SADECE bu case için hiç SLA_ASIMI puan hareketi
    /// yazılmamışsa uygulanır (PointRuleEngine.hadPriorSlaBreach parametresi buradan beslenir).
    /// </summary>
    Task<bool> HasSlaBreachForCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>MARATONCU rozeti (bir günde 20 optimizasyon) — [dayStart, dayEndExclusive) aralığında
    /// OPTIMIZASYON_TAMAMLANDI sayısı (bu çağrının kendisi dahil, satır zaten eklenmiş olmalı).</summary>
    Task<int> CountTodayCompletionsAsync(
        Guid expertId, DateTimeOffset dayStart, DateTimeOffset dayEndExclusive, CancellationToken cancellationToken = default);
}
