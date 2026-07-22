using System.Text.Json;
using BuildingBlocks.Events;
using FluentAssertions;
using Xunit;

namespace BuildingBlocks.UnitTests;

public class IntegrationEventTests
{
    private sealed record SamplePayload
    {
        public required string CaseId { get; init; }
    }

    private sealed record SampleEvent : IntegrationEvent<SamplePayload>
    {
        public override string EventType => EventTypes.CaseCreated;
    }

    [Fact]
    public void Zarf_snake_case_alanlara_serilesmeli_Python_interop_sarti()
    {
        var evt = new SampleEvent { Payload = new SamplePayload { CaseId = "abc-123" } };

        var json = JsonSerializer.Serialize<IntegrationEvent>(evt);

        json.Should().Contain("\"event_id\"");
        json.Should().Contain("\"event_type\"");
        json.Should().Contain("\"timestamp\"");
        json.Should().Contain("\"version\"");
        json.Should().Contain("\"payload\"");
        json.Should().Contain("case.created");
    }

    [Fact]
    public void EventTypes_sabitleri_Core_Principles_katalogu_ile_birebir_olmali()
    {
        EventTypes.CampaignCreated.Should().Be("campaign.created");
        EventTypes.CaseCreated.Should().Be("case.created");
        EventTypes.CaseAssigned.Should().Be("case.assigned");
        EventTypes.CaseStatusChanged.Should().Be("case.status_changed");
        EventTypes.CampaignOptimized.Should().Be("campaign.optimized");
        EventTypes.CaseSlaBreached.Should().Be("case.sla_breached");
        EventTypes.SegmentOverridden.Should().Be("segment.overridden");
        EventTypes.OfferResponded.Should().Be("offer.responded");
        EventTypes.OfferRated.Should().Be("offer.rated");
        EventTypes.BadgeEarned.Should().Be("badge.earned");
        EventTypes.PointsUpdated.Should().Be("points.updated");
    }
}
