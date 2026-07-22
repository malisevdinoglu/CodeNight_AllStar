using System.Text;
using System.Text.Json;
using BuildingBlocks.Behaviors;
using BuildingBlocks.Messaging;
using BuildingBlocks.Middleware;
using FluentValidation;
using Identity.Api.Http;
using Identity.Api.Security;
using Identity.Application;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Extensions;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence.Seeding;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;

// Core_Principles §1: Serilog (structured, JSON). Ana host henuz hazir olmadan
// once erken bir logger kurulu olsun ki startup hatalari da yakalanabilsin.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

// JWT claim isimlerini oldugu gibi koru (sub/role/expertise) — Gateway ile birebir ayni
// davranis (DefaultInboundClaimTypeMap.Clear()), aksi halde [Authorize(Roles=...)] calismaz.
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IDataSeeder, IdentityDataSeeder>();

// ---- Application katmani: repository/security/unit-of-work (Faz 4) ----
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentRequestContext, HttpCurrentRequestContext>();
builder.Services.AddScoped<InternalApiKeyFilter>();

// ---- MediatR + cross-cutting pipeline (BuildingBlocks) ----
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

// ---- Kimlik dogrulama: Core_Principles §6 — Gateway ile AYNI Issuer/Audience/Secret.
// Defense in depth: Gateway zaten dogruladi, Identity kendi basina da dogrular (Core_Principles §6). ----
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
        // BUG FIX: statik JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear() (yukarida)
        // tek basina guvenilir degil — .NET 8'in JwtBearer'i varsayilan olarak
        // Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler kullanabiliyor, bu da
        // System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler'in statik haritasindan
        // BAGIMSIZ calisabiliyor. Canli testte dogrulandi: JWT gecerli sekilde dogrulaniyor
        // ([Authorize] geciyor, custom OnChallenge tetiklenmiyor) ama ClaimsPrincipal'da "sub"
        // FindFirst("sub") ile bulunamiyordu (GetMeQueryHandler UNAUTHENTICATED atiyordu) —
        // claim turu sessizce baska bir ada (ClaimTypes.NameIdentifier) eslenmis olmaliydi.
        // MapInboundClaims = false, kullanilan handler ne olursa olsun eslemeyi kesin olarak
        // kapatir ve token'daki ham claim adlarini ("sub"/"role") korur.
        options.MapInboundClaims = false;

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

        // 401/403 govdelerini de ApiResponse zarfina uydur (Core_Principles §5).
        // Her 403 -> audit log (Core_Principles §6) — burada merkezi olarak yazilir,
        // her handler'da tekrar etmeye gerek kalmaz.
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
                        code = "AUTH_401_UNAUTHORIZED",
                        message = "Kimlik dogrulama basarisiz veya token suresi dolmus.",
                        details = Array.Empty<string>()
                    }
                }));
            },
            OnForbidden = async context =>
            {
                try
                {
                    var userIdRaw = context.HttpContext.User.FindFirst("sub")?.Value;
                    var userId = Guid.TryParse(userIdRaw, out var parsed) ? (Guid?)parsed : null;
                    var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    var auditLogRepository = context.HttpContext.RequestServices.GetRequiredService<IAuditLogRepository>();
                    var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

                    auditLogRepository.Add(AuditLog.Create(
                        userId, AuditActionType.ACCESS_DENIED, DateTime.UtcNow, ip,
                        success: false, resourceId: context.HttpContext.Request.Path, details: null));
                    await unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "403 audit log yazilamadi");
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    data = (object?)null,
                    error = new
                    {
                        code = "AUTH_403_FORBIDDEN",
                        message = "Bu kaynaga erisim yetkiniz yok.",
                        details = Array.Empty<string>()
                    }
                }));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ---- Swagger/OpenAPI (Mali_Plan.md Faz 9: her .NET serviste /swagger) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CampaignCell Identity API",
        Version = "v1",
        Description = "Kimlik dogrulama, personel/musteri yonetimi, audit log (Core_Principles §6).",
    });

    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Ornek: 'Bearer eyJhbGciOi...' (POST /api/v1/auth/login yanitindaki accessToken).",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

// ---- Event bus (Core_Principles §8) ----
builder.Services.AddCampaignCellMassTransit(builder.Configuration);

var app = builder.Build();

// Programatik migration + seed (Core_Principles §9: tek komut sarti)
await app.Services.MigrateAndSeedAsync();

// Tum hatalar tek merkezden ApiResponse zarfina cevrilir (Core_Principles §5)
app.UseCampaignCellExceptionHandling("AUTH");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "identity" },
    error = (object?)null
}));

app.Run();
