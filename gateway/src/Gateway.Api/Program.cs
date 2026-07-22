using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

// JWT claim isimlerini oldugu gibi koru (sub/role/expertise) — .NET'in otomatik
// XML-schema claim eslemesini (mesela "sub" -> uzun bir URI) devre disi birak.
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "Jwt:Secret tanimli degil. JWT_SECRET ortam degiskenini (.env) ayarlayin.");
}

// ---- Kimlik dogrulama: Core_Principles §6 access token sozlesmesi ----
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

        // 401/403 gövdelerini de ApiResponse zarfına uydur (Core_Principles §5).
        options.Events = new JwtBearerEvents
        {
            // Faz 7: GameHub (SignalR) WebSocket el sikismasi Authorization header'i TASIYAMAZ
            // (tarayici WS upgrade istegine ozel header ekleyemez) - token query string'te gelir
            // (?access_token=...). Gamification.Api'de de (defense in depth) ayni destek var.
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
                        code = "GATEWAY_401_UNAUTHORIZED",
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
                        code = "GATEWAY_403_FORBIDDEN",
                        message = "Bu kaynaga erisim yetkiniz yok.",
                        details = Array.Empty<string>()
                    }
                }));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Route bazli politika: Core_Principles §2 anonim path listesi disindaki
    // her sey gecerli (imza+sure dogrulanmis) bir JWT ister.
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// ---- CORS: sadece frontend origin'i (Core_Principles §9) ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy => policy
        .WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---- Rate limiting: global 60/dk (IP) + login/otp-verify icin 5/dk (Core_Principles §10) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("auth-strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientIp(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            data = (object?)null,
            error = new
            {
                code = "GATEWAY_429_RATE_LIMIT_EXCEEDED",
                message = "Istek limiti asildi. Lutfen bir sure sonra tekrar deneyin.",
                details = Array.Empty<string>()
            }
        }), token);
    };
});

// ---- YARP: route/cluster tablosu appsettings.json'dan (Core_Principles §9) ----
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors("frontend");

// Anti-spoofing: client'tan gelen kimlik header'larini HER ZAMAN sil.
// (Asagidaki middleware, JWT gecerliyse bunlari kendi dogruladigi degerlerle yeniden yazar.)
app.Use(async (context, next) =>
{
    context.Request.Headers.Remove("X-User-Id");
    context.Request.Headers.Remove("X-User-Role");
    context.Request.Headers.Remove("X-User-Expertise");
    await next();
});

app.UseRateLimiter();

app.UseAuthentication();

// JWT gecerliyse dogrulanmis claim'leri downstream servislere header olarak ekle.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var role = context.User.FindFirst("role")?.Value;
        var expertise = context.User.FindAll("expertise").Select(c => c.Value).ToArray();

        if (!string.IsNullOrEmpty(userId))
        {
            context.Request.Headers["X-User-Id"] = userId;
        }

        if (!string.IsNullOrEmpty(role))
        {
            context.Request.Headers["X-User-Role"] = role;
        }

        if (expertise.Length > 0)
        {
            context.Request.Headers["X-User-Expertise"] = string.Join(",", expertise);
        }
    }

    await next();
});

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    data = new { status = "Healthy", service = "gateway" },
    error = (object?)null
}));

app.MapReverseProxy();

app.Run();

static string GetClientIp(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
