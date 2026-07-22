using Gamification.Infrastructure.Extensions;
using Gamification.Infrastructure.Persistence;
using Gamification.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GamificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, GamificationDataSeeder>();

var app = builder.Build();

// Programatik migration + seed (Core_Principles §9: tek komut sarti)
await app.Services.MigrateAndSeedAsync();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "gamification" },
    error = (object?)null
}));

app.Run();
