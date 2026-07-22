namespace Campaign.Infrastructure.External;

/// <summary>
/// Core_Principles §6: servisler-arası çağrı Gateway'i atlar, container ağından doğrudan gider,
/// X-Internal-Api-Key ile korunur (JWT değil). BaseUrl sabit docker-network host adı olduğu için
/// appsettings.json'da; ApiKey ise gizli olduğundan .env → docker-compose → INTERNAL_API_KEY.
/// </summary>
public sealed class IdentityServiceOptions
{
    public const string SectionName = "IdentityService";

    public string BaseUrl { get; set; } = string.Empty;
}
