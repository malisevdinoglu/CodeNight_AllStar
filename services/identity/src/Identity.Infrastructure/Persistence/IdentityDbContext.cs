using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Identity servisinin DbContext'i (database-per-service: SADECE kendi DB'sine baglanir).
/// Entity konfigurasyonlari bu assembly'deki IEntityTypeConfiguration
/// implementasyonlarindan otomatik yuklenir (Iskender'in alani).
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
