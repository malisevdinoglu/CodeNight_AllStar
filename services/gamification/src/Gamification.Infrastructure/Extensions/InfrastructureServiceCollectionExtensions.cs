using Gamification.Application.Common;
using Gamification.Application.External;
using Gamification.Infrastructure.Common;
using Gamification.Infrastructure.External;
using Gamification.Infrastructure.Persistence.Repositories;
using Gamification.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gamification.Infrastructure.Extensions;

/// <summary>
/// Repository/UnitOfWork/dış servis istemcilerinin tek noktadan DI kaydı (Campaign.Infrastructure
/// InfrastructureServiceCollectionExtensions ile aynı desen). DbContext kaydı + migration/seed
/// çağrısı Program.cs'de kalır (Identity/Campaign ile tutarlı).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    private const string InternalApiKeyHeader = "X-Internal-Api-Key";

    public static IServiceCollection AddGamificationInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IdentityServiceOptions>(configuration.GetSection(IdentityServiceOptions.SectionName));

        services.AddScoped<IExpertScoreRepository, ExpertScoreRepository>();
        services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
        services.AddScoped<ISegmentCompletionCountRepository, SegmentCompletionCountRepository>();
        services.AddScoped<IExpertBadgeRepository, ExpertBadgeRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Identity: Core_Principles §6 - Gateway atlanir, X-Internal-Api-Key ile korunur.
        // Sadece "cold path" isim cozumu icin kullanilir (bkz. IdentityServiceClient).
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((sp, client) =>
        {
            var options = configuration.GetSection(IdentityServiceOptions.SectionName).Get<IdentityServiceOptions>()
                ?? new IdentityServiceOptions();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add(InternalApiKeyHeader, configuration["INTERNAL_API_KEY"] ?? string.Empty);
        });

        // Redis: RABBITMQ_HOST ile ayni desen - duz env degiskeni (compose'ta REDIS_HOST zaten var).
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = new RedisOptions
            {
                Host = configuration["REDIS_HOST"] ?? "redis",
                Port = int.TryParse(configuration["REDIS_PORT"], out var parsedPort) ? parsedPort : 6379,
            };
            return ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { { options.Host, options.Port } },
                AbortOnConnectFail = false,
            });
        });
        services.AddScoped<ILeaderboardService, RedisLeaderboardService>();

        return services;
    }
}
