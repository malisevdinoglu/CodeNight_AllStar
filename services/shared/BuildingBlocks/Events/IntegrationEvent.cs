using System.Text.Json.Serialization;

namespace BuildingBlocks.Events;

/// <summary>
/// Tüm entegrasyon event'lerinin ortak zarfı (Core_Principles §8).
/// JSON alanları KASITLI olarak snake_case — Python (aio-pika) interop şartı.
/// MassTransit bu tipleri <c>UseRawJsonSerializer()</c> ile yayınlar, yani
/// MassTransit'e özel zarf eklenmez; JSON tam olarak bu şekle serileşir.
/// </summary>
public abstract record IntegrationEvent
{
    [JsonPropertyName("event_id")]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("event_type")]
    public abstract string EventType { get; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;
}

/// <summary>
/// Somut event'ler bunu implemente eder; <typeparamref name="TPayload"/> event'e özel
/// alanları taşır. Örnek (Faz 5, Campaign.Application.Events içinde):
/// <code>
/// public sealed record CampaignOptimizedEvent : IntegrationEvent&lt;CampaignOptimizedPayload&gt;
/// {
///     public override string EventType => EventTypes.CampaignOptimized;
/// }
/// </code>
/// </summary>
public abstract record IntegrationEvent<TPayload> : IntegrationEvent
{
    [JsonPropertyName("payload")]
    public required TPayload Payload { get; init; }
}
