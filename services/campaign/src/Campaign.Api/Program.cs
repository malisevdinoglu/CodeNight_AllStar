using Campaign.Infrastructure.Extensions;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CampaignDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, NoOpDataSeeder>();

var app = builder.Build();

// Programatik migration + seed (Core_Principles §9: tek komut sarti)
await app.Services.MigrateAndSeedAsync();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "campaign" },
    error = (object?)null
}));

app.Run();
