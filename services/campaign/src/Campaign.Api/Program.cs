using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Behaviors;
using BuildingBlocks.Messaging;
using BuildingBlocks.Middleware;
using Campaign.Api.BackgroundServices;
using Campaign.Api.Http;
using Campaign.Application;
using Campaign.Application.Common;
using Campaign.Infrastructure.Extensions;
using Campaign.Infrastructure.Persistence;
using Campaign.Infrastructure.Persistence.Seeding;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

// Identity.Api ile AYNI gerekce: JWT claim isimlerini (sub/role/expertise) oldugu gibi koru,
// aksi halde [Authorize(Roles=...)] ve ICurrentRequestContext claim okumalari calismaz.
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<CampaignDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, CampaignDataSeeder>();

// ---- Infrastructure: repository/unit-of-work/dis servis istemcileri (Faz 5) ----
builder.Services.AddCampaignInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();

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

        // 401/403 govdelerini ApiResponse zarfina uydur (Core_Principles §5).
        options.Events = new JwtBearerEvents
        {
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
                        code = "CMP_401_UNAUTHORIZED",
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
                        code = "CMP_403_FORBIDDEN",
                        message = "Bu kaynaga erisim yetkiniz yok.",
                        details = Array.Empty<string>()
                    }
                }));
            }
        };
    });

builder.Services.AddAuthorization();

// Enum'lar frontend ile birebir string olarak tasinir (ör. "YENI", "EK_PAKET") - sayisal
// index DEGIL. Hem istek govdesi binding'i hem yanit DTO'lari (CaseDto, OfferDto vb.) bunu bekler.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---- Event bus + Outbox (Core_Principles §2/§8): DB commit + publish atomik ----
builder.Services.AddCampaignCellMassTransit<CampaignDbContext>(builder.Configuration);

// ---- SLA Worker (Mali_Plan.md: dakikalik BackgroundService) ----
builder.Services.AddHostedService<SlaSweepBackgroundService>();

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

app.UseCampaignCellExceptionHandling("CMP");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "campaign" },
    error = (object?)null
}));

app.Run();
