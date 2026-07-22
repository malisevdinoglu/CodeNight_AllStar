using System.Text.Json.Serialization;
using BuildingBlocks.Events;

namespace Gamification.Application.Events;

/// <summary>
/// Gamification servisinin TÜKETTİĞİ event kontratları (Core_Principles §8 event kataloğu).
/// Database-per-service ilkesi gereği tipler Campaign.Application.Events ile PAYLAŞILMAZ —
/// burada bağımsızca yeniden tanımlanır, ancak tel (wire) JSON şekli Campaign'in yayınladığıyla
/// BİREBİR aynı olmak zorundadır (alan adları, snake_case, tipler). <see cref="IntegrationEvent{TPayload}"/>
/// zarfı BuildingBlocks'tan (paylaşılan altyapı katmanı) gelir — bu paylaşım kuralın istisnası değil,
/// zarfın kendisi domain/servis'e özgü değildir.
/// </summary>
public sealed record CampaignOptimizedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("expert_id")] Guid ExpertId,
    [property: JsonPropertyName("segment")] string Segment,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("conversion_lift")] decimal? ConversionLift,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("completed_at")] DateTimeOffset CompletedAt);

public sealed record CampaignOptimizedEvent : IntegrationEvent<CampaignOptimizedPayload>
{
    public override string EventType => EventTypes.CampaignOptimized;
}

public sealed record CaseSlaBreachedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("expert_id")] Guid? ExpertId,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("breached_at")] DateTimeOffset BreachedAt);

public sealed record CaseSlaBreachedEvent : IntegrationEvent<CaseSlaBreachedPayload>
{
    public override string EventType => EventTypes.CaseSlaBreached;
}

public sealed record OfferRatedPayload(
    [property: JsonPropertyName("offer_id")] Guid OfferId,
    [property: JsonPropertyName("subscriber_id")] Guid SubscriberId,
    [property: JsonPropertyName("expert_id")] Guid? ExpertId,
    [property: JsonPropertyName("campaign_id")] Guid CampaignId,
    [property: JsonPropertyName("stars")] int Stars);

public sealed record OfferRatedEvent : IntegrationEvent<OfferRatedPayload>
{
    public override string EventType => EventTypes.OfferRated;
}
