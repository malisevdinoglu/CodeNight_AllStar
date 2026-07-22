using BuildingBlocks.Events;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Her IntegrationEvent yayınının routing key'ini otomatik olarak event_type
/// yapan tek giriş noktası. Faz 5/7'deki tüm publish çağrıları bunu kullanmalı,
/// çıplak <c>IPublishEndpoint.Publish</c> DEĞİL — aksi halde routing key boş
/// kalır ve Python tarafı mesajı yakalayamaz.
/// </summary>
public static class PublishEndpointExtensions
{
    public static Task PublishIntegrationEventAsync<TEvent>(
        this IPublishEndpoint publishEndpoint,
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        return publishEndpoint.Publish(
            integrationEvent,
            context => context.SetRoutingKey(integrationEvent.EventType),
            cancellationToken);
    }
}
