using BuildingBlocks.Behaviors;
using BuildingBlocks.Messaging;
using BuildingBlocks.Middleware;
using FluentValidation;
using Gamification.Application;
using Gamification.Infrastructure.Extensions;
using Gamification.Infrastructure.Persistence;
using Gamification.Infrastructure.Persistence.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<GamificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, GamificationDataSeeder>();

// ---- MediatR + cross-cutting pipeline (BuildingBlocks) ----
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

// ---- Event bus (Core_Principles §8) — yazma tamamen event-driven, consumer'lar Faz 7'de ----
builder.Services.AddCampaignCellMassTransit(builder.Configuration);

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

app.UseCampaignCellExceptionHandling("GAM");

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "gamification" },
    error = (object?)null
}));

app.Run();
