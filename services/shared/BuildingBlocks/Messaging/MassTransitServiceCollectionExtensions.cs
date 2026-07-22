using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

/// <summary>
/// RabbitMQ + MassTransit için tek DI giriş noktası. Bağlantı bilgileri env'den
/// gelir (RABBITMQ_HOST/USER/PASSWORD — compose'ta zaten tanımlı).
/// <c>UseRawJsonSerializer()</c> ZORUNLU: Python (aio-pika) tarafı MassTransit'in
/// kendi zarf formatını anlamaz, ham JSON bekler (Core_Principles §8 interop şartı).
/// </summary>
public static class MassTransitServiceCollectionExtensions
{
    public static IServiceCollection AddCampaignCellMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(busConfigurator =>
        {
            configureConsumers?.Invoke(busConfigurator);

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RABBITMQ_HOST"] ?? "rabbitmq";
                var user = configuration["RABBITMQ_USER"] ?? "guest";
                var pass = configuration["RABBITMQ_PASSWORD"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });

                cfg.UseRawJsonSerializer();

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
