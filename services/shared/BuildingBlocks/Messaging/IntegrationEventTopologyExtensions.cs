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

    /// <summary>
    /// Tüketici tarafı: dayanıklı bir kuyruk oluşturur, ortak topic exchange'e SADECE verilen
    /// routing key (= event_type) ile bağlar. <c>cfg.ConfigureEndpoints(context)</c>'in aksine
    /// (mesaj tipinin varsayılan adlandırma kuralına göre otomatik kuyruk/binding üretir ve
    /// topic exchange + özel routing key senaryosunda YANLIŞ bağlanır), bu, Campaign'in
    /// <see cref="PublishEndpointExtensions.PublishIntegrationEventAsync{TEvent}"/> ile yayınladığı
    /// routing key'e BİREBİR eşleşen açık bir binding kurar (Faz 7, Gamification consumer'ları).
    /// </summary>
    public static void ReceiveIntegrationEvent<TEvent, TConsumer>(
        this IRabbitMqBusFactoryConfigurator cfg,
        IBusRegistrationContext context,
        string queueName,
        string routingKey)
        where TEvent : class
        where TConsumer : class, IConsumer<TEvent>
    {
        // BILINCLI olarak cfg.Message<TEvent>(x => x.SetEntityName(...)) YOK: bu servis TEvent'i
        // hic publish etmiyor (sadece tuketiyor). O cagri, "campaigncell.events" adini VARSAYILAN
        // (fanout) tipiyle deklare etmeye calisir ve asagidaki e.Bind'in ayni ismi ACIKCA "topic"
        // olarak deklare etmesiyle CATISIR - MassTransit bus olusturma aninda
        // "entity settings did not match the existing entity" hatasiyla patlar (canli testte
        // dogrulandi). e.Bind tek basina, kuyruyu ortak topic exchange'e verilen routing key ile
        // baglamak icin yeterli ve dogrudur.
        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.ConfigureConsumer<TConsumer>(context);
            e.Bind(EventsExchangeName, x =>
            {
                x.ExchangeType = "topic";
                x.RoutingKey = routingKey;
            });
        });
    }
}
