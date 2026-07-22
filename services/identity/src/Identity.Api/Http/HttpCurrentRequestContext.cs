using Identity.Application.Common;

namespace Identity.Api.Http;

/// <summary>
/// ICurrentRequestContext'in Api implementasyonu (Clean Architecture: Application katmanı
/// HttpContext'i bilmez). Claim'ler HttpContext.User'dan okunur — Gateway zaten JWT'yi
/// doğrulayıp X-User-* header'larını enjekte eder, ama Identity kendi JwtBearer'ıyla da
/// AYNI token'ı bağımsızca doğrular (Core_Principles §6: defense in depth) ve context'i
/// güvenilir tek kaynaktan (validated ClaimsPrincipal) türetir — spoofable header'lardan değil.
/// </summary>
public sealed class HttpCurrentRequestContext : ICurrentRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value;

    /// <summary>
    /// Gateway arkasında çalışıldığı için gerçek istemci IP'si X-Forwarded-For'da olur;
    /// yoksa (doğrudan çağrı / test) bağlantı IP'sine düşer.
    /// </summary>
    public string IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "unknown";
            }

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
