using Identity.Domain.Entities;

namespace Identity.Application.Common;

public sealed record AccessTokenResult(string Token, string Jti, DateTime ExpiresAtUtc);

/// <summary>
/// Core_Principles §6 payload sözleşmesi: sub, role, expertise[], region, jti, exp (15 dk).
/// Gateway (Faz 2) ile AYNI Issuer/Audience/Secret kullanılmalı — aksi halde 401.
/// </summary>
public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
}
