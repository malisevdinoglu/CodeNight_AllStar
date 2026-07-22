using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Behaviors;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using BuildingBlocks.Middleware;
using FluentValidation;
using Gamification.Api.Consumers;
using Gamification.Api.Realtime;
using Gamification.Application;
using Gamification.Application.Common;
using Gamification.Application.Events;
using Gamification.Infrastructure.Extensions;
using Gamification.Infrastructure.Persistence;
using Gamification.Infrastructure.Persistence.Seeding;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

// Identity/Campaign ile AYNI gerekce: JWT claim isimlerini (sub/role) oldugu gibi koru.
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<GamificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, GamificationDataSeeder>();
builder.Services.AddGamificationInfrastructure(builder.Configuration);

// ---- MediatR + cross-cutting pipeline (BuildingBlocks) ----
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

// ---- Kimlik dogrulama: Gateway ile AYNI Issuer/Audience/Secret (Core_Principles §6 defense in depth) ----
var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "Jwt:Secret tanimli degil. JWT_SECRET ortam degiskenini (.env) ayarlayin.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        // SignalR WebSocket el sikismasi Authorization header'i TASIYAMAZ - token query
        // string'te gelir (?access_token=...). Gateway'de de (Task #39) ayni destek eklenir;
        // burada tekrar dogrulanmasi defense-in-depth (Core_Principles §6).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "GAM_401_UNAUTHORIZED",
                        message = "Kimlik dogrulama basarisiz veya token suresi dolmus.",
                        details = Array.Empty<string>()
                    }
                }));
            },
            OnForbidden = async context =>
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "GAM_403_FORBIDDEN",
                        message = "Bu kaynaga erisim yetkiniz yok.",
                        details = Array.Empty<string>()
                    }
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// Enum'lar frontend ile birebir string olarak tasinir (ör. "BRONZ", "PLATIN") - sayisal index DEGIL.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---- SignalR: badge.earned / points.updated SUNUCU->İSTEMCİ push (Core_Principles §8) ----
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
builder.Services.AddScoped<IGameNotifier, GameNotifier>();
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---- Event bus (Core_Principles §8): SADECE tuketici (outbox YOK - Gamification RabbitMQ'ya
// yayin yapmaz, badge.earned/points.updated dogrudan SignalR push'tur). ----
builder.Services.AddCampaignCellMassTransit(
    builder.Configuration,
    configureConsumers: cfg =>
    {
        cfg.AddConsumer<CampaignOptimizedConsumer>();
        cfg.AddConsumer<CaseSlaBreachedConsumer>();
        cfg.AddConsumer<OfferRatedConsumer>();
    },
    configureRabbitMq: (context, cfg) =>
    {
        cfg.ReceiveIntegrationEvent<CampaignOptimizedEvent, CampaignOptimizedConsumer>(
            context, "gamification.campaign-optimized", EventTypes.CampaignOptimized);
        cfg.ReceiveIntegrationEvent<CaseSlaBreachedEvent, CaseSlaBreachedConsumer>(
            context, "gamification.case-sla-breached", EventTypes.CaseSlaBreached);
        cfg.ReceiveIntegrationEvent<OfferRatedEvent, OfferRatedConsumer>(
            context, "gamification.offer-rated", EventTypes.OfferRated);
    });

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

app.UseCampaignCellExceptionHandling("GAM");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "gamification" },
    error = (object?)null
}));

app.Run();
