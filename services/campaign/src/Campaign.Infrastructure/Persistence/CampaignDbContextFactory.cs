using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// "dotnet ef migrations add" tasarım-zamanı fabrikası — Identity.Infrastructure'daki
/// IdentityDbContextFactory ile AYNI gerekçe: bunsuz `dotnet ef`, Campaign.Api'nin Program.cs'ini
/// çalıştırmaya çalışır ve docker-only host adına ("campaign-db") bağlanamayıp donar/patlar.
/// Bağlantı GERÇEK olmak zorunda değil — sadece şema karşılaştırması için model gerekir.
/// </summary>
public sealed class CampaignDbContextFactory : IDesignTimeDbContextFactory<CampaignDbContext>
{
    public CampaignDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CampaignDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5434;Database=campaign_db;Username=campaign_user;Password=design_time_only")
            .UseSnakeCaseNamingConvention();

        return new CampaignDbContext(optionsBuilder.Options);
    }
}
