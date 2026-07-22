using Campaign.Application.Common;
using Campaign.Application.External;
using Campaign.Infrastructure.Common;
using Campaign.Infrastructure.External;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Campaign.Infrastructure.Extensions;

/// <summary>
/// Repository/UnitOfWork/dış servis istemcilerinin tek noktadan DI kaydı (Identity.Infrastructure
/// ile aynı desen). DbContext + outbox kaydı Program.cs'de kalır (AddEntityFrameworkOutbox,
/// MassTransit bus konfigürasyonuyla birlikte tek yerde yönetilmesi daha okunaklı).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string InternalApiKeyHeader = "X-Internal-Api-Key";

    public static IServiceCollection AddCampaignInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityServiceOptions>(configuration.GetSection(IdentityServiceOptions.SectionName));
        services.Configure<AiServiceOptions>(configuration.GetSection(AiServiceOptions.SectionName));

        services.AddScoped<ICampaignRepository, CampaignRepository>();
        services.AddScoped<IOptimizationCaseRepository, OptimizationCaseRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IOfferRatingRepository, OfferRatingRepository>();
        services.AddScoped<ISubscriberProfileRepository, SubscriberProfileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INumberSequenceProvider, CampaignNumberSequenceProvider>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // AI: 3 sn timeout (Core_Principles §3) - graceful degradation'in ilk savunma hatti.
        services.AddHttpClient<IAiServiceClient, AiServiceClient>((sp, client) =>
        {
            var options = configuration.GetSection(AiServiceOptions.SectionName).Get<AiServiceOptions>()
                ?? new AiServiceOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        // Identity: Core_Principles §6 - Gateway atlanir, X-Internal-Api-Key ile korunur.
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((sp, client) =>
        {
            var options = configuration.GetSection(IdentityServiceOptions.SectionName).Get<IdentityServiceOptions>()
                ?? new IdentityServiceOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add(InternalApiKeyHeader, configuration["INTERNAL_API_KEY"] ?? string.Empty);
        });

        return services;
    }
}
