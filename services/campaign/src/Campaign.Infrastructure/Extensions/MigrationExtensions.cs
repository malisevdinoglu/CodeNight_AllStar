using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Campaign.Infrastructure.Extensions;

/// <summary>
/// Programatik migration + seed: "dotnet ef database update" yerine uygulama
/// acilisinda calisir (docker compose up ile tek komutta ayaga kalkma sarti).
/// </summary>
public static class MigrationExtensions
{
    public static async Task MigrateAndSeedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migration");
        var dbContext = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();

        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migration tamamlandi.");
                break;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Veritabani henuz hazir degil (deneme {Attempt}/{MaxAttempts}), 3 sn sonra tekrar denenecek.",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Seed tamamlandi.");
    }
}
