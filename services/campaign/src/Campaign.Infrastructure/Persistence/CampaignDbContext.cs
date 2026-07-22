using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// Campaign servisinin DbContext'i (database-per-service: SADECE kendi DB'sine baglanir).
/// Entity konfigurasyonlari bu assembly'deki IEntityTypeConfiguration
/// implementasyonlarindan otomatik yuklenir (Iskender'in alani).
/// </summary>
public class CampaignDbContext : DbContext
{
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampaignDbContext).Assembly);
    }
}
