using FluentAssertions;
using Campaign.Application;
using Campaign.Infrastructure.Persistence.Seeding;
using Xunit;

namespace Campaign.UnitTests;

/// <summary>
/// Iskelet dogrulamasi: katman referanslarinin dogru kuruldugunu garanti eder.
/// Gercek is kurali testleri ilgili fazlarda eklenir.
/// </summary>
public class ArchitectureSmokeTests
{
    [Fact]
    public void Application_assembly_referansi_dogru_kurulmus_olmali()
    {
        typeof(ApplicationAssemblyMarker).Assembly.GetName().Name
            .Should().Be("Campaign.Application");
    }

    [Fact]
    public async Task NoOpDataSeeder_hatasiz_calismali()
    {
        var seeder = new NoOpDataSeeder();
        var act = async () => await seeder.SeedAsync();
        await act.Should().NotThrowAsync();
    }
}
