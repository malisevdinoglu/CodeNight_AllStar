using Campaign.Application.Common;

namespace Campaign.Api.Http;

/// <summary>
/// ICurrentRequestContext'in Api implementasyonu — Identity.Api.HttpCurrentRequestContext ile
/// aynı desen: claim'ler yalnızca doğrulanmış HttpContext.User'dan okunur (spoofable header'dan
/// değil). Campaign ayrıca "expertise" (çoklu claim) okur - PERSONEL'in uzmanlık alanları.
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

    public IReadOnlyList<string> Expertise =>
        _httpContextAccessor.HttpContext?.User.FindAll("expertise").Select(c => c.Value).ToList()
        ?? new List<string>();

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
