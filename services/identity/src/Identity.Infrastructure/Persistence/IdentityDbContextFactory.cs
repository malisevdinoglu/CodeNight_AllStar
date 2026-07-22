using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// "dotnet ef migrations add" tasarım-zamanı fabrikası. ÖNEMLİ: Bunsuz `dotnet ef`,
/// Identity.Api'nin Program.cs'ini (host factory resolver ile) çalıştırmaya çalışır —
/// bu da <c>app.Services.MigrateAndSeedAsync()</c> satırının migration scaffolding
/// sırasında tetiklenip "identity-db" host adını (sadece docker network'ünde çözülür)
/// bağlanmaya çalışmasına, yani migration komutunun donmasına/patlamasına yol açar.
/// Bu factory varken `dotnet ef` doğrudan bunu kullanır, Program.cs hiç çalışmaz.
/// Bağlantı burada GERÇEK olmak zorunda değil — sadece şema karşılaştırması için model gerekir.
/// </summary>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5433;Database=identity_db;Username=identity_user;Password=design_time_only")
            .UseSnakeCaseNamingConvention();

        return new IdentityDbContext(optionsBuilder.Options);
    }
}
