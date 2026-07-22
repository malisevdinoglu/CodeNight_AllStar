namespace Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Baslangic verisi sozlesmesi. Gercek implementasyon Iskender tarafindan yazilir;
/// Program.cs'teki DI kaydi NoOpDataSeeder yerine gercek seeder ile degistirilir.
/// </summary>
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
