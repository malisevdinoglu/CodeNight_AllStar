namespace Identity.Application.Common;

/// <summary>
/// HTTP isteğinden türeyen bağlam (JWT claim'leri + IP) — Api katmanı implemente eder.
/// Application katmanı HttpContext'i doğrudan bilmez (Clean Architecture: Api → Application yönü).
/// </summary>
public interface ICurrentRequestContext
{
    Guid? UserId { get; }

    string? Role { get; }

    /// <summary>Audit log + hesap kilitleme/rate-limit bağlamı için istemci IP'si.</summary>
    string IpAddress { get; }
}
