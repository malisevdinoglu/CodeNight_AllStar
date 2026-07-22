using MassTransit;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Core_Principles §8: TÜM event'ler AYNI topic exchange'e ("campaigncell.events")
/// yayınlanır — MassTransit'in varsayılanı olan "her mesaj tipi kendi exchange'i"
/// davranışının BİLİNÇLİ olarak devre dışı bırakılması. Routing key = event_type,
/// bu da yayın anında <see cref="PublishEndpointExtensions.PublishIntegrationEventAsync{TEvent}"/>
/// ile ayarlanır.
///
/// Kullanım (Faz 5/7, her somut event türü için BİR KEZ, bus konfigürasyonunda):
/// <code>cfg.ConfigureIntegrationEventTopology&lt;CampaignOptimizedEvent&gt;();</code>
/// </summary>
public static class IntegrationEventTopologyExtensions
{
    public const string EventsExchangeName = "campaigncell.events";

    public static void ConfigureIntegrationEventTopology<TEvent>(this IRabbitMqBusFactoryConfigurator cfg)
        where TEvent : class
    {
        cfg.Message<TEvent>(x => x.SetEntityName(EventsExchangeName));
        cfg.Publish<TEvent>(x => x.ExchangeType = "topic");
    }
}
