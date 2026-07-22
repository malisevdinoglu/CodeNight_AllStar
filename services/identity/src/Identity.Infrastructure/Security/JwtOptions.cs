namespace Identity.Infrastructure.Security;

/// <summary>
/// appsettings "Jwt" bölümüne bağlanır. Gateway (Faz 2) ile Issuer/Audience/Secret AYNI
/// olmalı — aksi halde Gateway 401 döner (Core_Principles §6).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
}
