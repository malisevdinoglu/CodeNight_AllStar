using System.Text.Json.Serialization;
using BuildingBlocks.Events;

namespace Campaign.Application.Events;

/// <summary>
/// Core_Principles §8 event kataloğu — Campaign'in yayınladığı tüm event'ler.
/// Payload alanları KASITLI snake_case (Python interop, IntegrationEvent sözleşmesi).
/// </summary>
public sealed record CampaignCreatedPayload(
    [property: JsonPropertyName("campaign_id")] Guid CampaignId,
    [property: JsonPropertyName("campaign_number")] string CampaignNumber,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("target_segment")] string TargetSegment,
    [property: JsonPropertyName("created_by")] Guid CreatedBy);

public sealed record CampaignCreatedEvent : IntegrationEvent<CampaignCreatedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.CampaignCreated;
}

public sealed record CaseCreatedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("campaign_id")] Guid CampaignId,
    [property: JsonPropertyName("segment")] string Segment,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("sla_deadline")] DateTimeOffset SlaDeadline);

public sealed record CaseCreatedEvent : IntegrationEvent<CaseCreatedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.CaseCreated;
}

public sealed record CaseAssignedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("expert_id")] Guid ExpertId,
    [property: JsonPropertyName("assigned_by")] string AssignedBy);

public sealed record CaseAssignedEvent : IntegrationEvent<CaseAssignedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.CaseAssigned;
}

public sealed record CaseStatusChangedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("from_status")] string FromStatus,
    [property: JsonPropertyName("to_status")] string ToStatus,
    [property: JsonPropertyName("changed_by")] Guid ChangedBy);

public sealed record CaseStatusChangedEvent : IntegrationEvent<CaseStatusChangedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.CaseStatusChanged;
}

/// <summary>Gamification consumer'ı bunu dinler: TAMAMLANDI +10, süre&lt;2s +5, lift&gt;hedef +15, KRITIK&amp;SLA-içi +15.</summary>
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
    public override string EventType => BuildingBlocks.Events.EventTypes.CampaignOptimized;
}

/// <summary>Gamification consumer'ı bunu dinler: -5.</summary>
public sealed record CaseSlaBreachedPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("expert_id")] Guid? ExpertId,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("breached_at")] DateTimeOffset BreachedAt);

public sealed record CaseSlaBreachedEvent : IntegrationEvent<CaseSlaBreachedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.CaseSlaBreached;
}

/// <summary>AI consumer'ı bunu dinler: classification_feedback tablosuna yazar (doğruluk metriği).</summary>
public sealed record SegmentOverriddenPayload(
    [property: JsonPropertyName("case_id")] Guid CaseId,
    [property: JsonPropertyName("predicted_segment")] string PredictedSegment,
    [property: JsonPropertyName("corrected_segment")] string CorrectedSegment,
    [property: JsonPropertyName("changed_by")] Guid ChangedBy);

public sealed record SegmentOverriddenEvent : IntegrationEvent<SegmentOverriddenPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.SegmentOverridden;
}

/// <summary>AI consumer'ı bunu dinler: RET ise skor düşürme katsayısı günceller.</summary>
public sealed record OfferRespondedPayload(
    [property: JsonPropertyName("offer_id")] Guid OfferId,
    [property: JsonPropertyName("subscriber_id")] Guid SubscriberId,
    [property: JsonPropertyName("campaign_id")] Guid CampaignId,
    [property: JsonPropertyName("campaign_type")] string CampaignType,
    [property: JsonPropertyName("response")] string Response);

public sealed record OfferRespondedEvent : IntegrationEvent<OfferRespondedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.OfferResponded;
}

/// <summary>Gamification consumer'ı bunu dinler: 1-2★ → ilgili uzmana -3.</summary>
public sealed record OfferRatedPayload(
    [property: JsonPropertyName("offer_id")] Guid OfferId,
    [property: JsonPropertyName("subscriber_id")] Guid SubscriberId,
    [property: JsonPropertyName("expert_id")] Guid? ExpertId,
    [property: JsonPropertyName("campaign_id")] Guid CampaignId,
    [property: JsonPropertyName("stars")] int Stars);

public sealed record OfferRatedEvent : IntegrationEvent<OfferRatedPayload>
{
    public override string EventType => BuildingBlocks.Events.EventTypes.OfferRated;
}
