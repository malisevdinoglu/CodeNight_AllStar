namespace Gamification.Infrastructure.Persistence.Seeding;

/// <summary>
/// Gecici stub: gercek seed gelene kadar hicbir sey yapmaz.
/// </summary>
public sealed class NoOpDataSeeder : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
