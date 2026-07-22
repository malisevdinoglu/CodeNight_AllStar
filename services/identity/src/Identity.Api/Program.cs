using Identity.Infrastructure.Extensions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, IdentityDataSeeder>();

var app = builder.Build();

// Programatik migration + seed (Core_Principles §9: tek komut sarti)
await app.Services.MigrateAndSeedAsync();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "identity" },
    error = (object?)null
}));

app.Run();
