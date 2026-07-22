namespace Identity.Domain.Entities;

/// <summary>
/// Iskender.md §1 <c>refresh_tokens</c>. Rotation zinciri <see cref="ReplacedById"/> ile kurulur;
/// düz token ASLA saklanmaz — sadece SHA-256 hash'i (Core_Principles §10).
/// </summary>
public class RefreshToken
{
    public const int ExpiryDays = 7;

    protected RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt, string createdByIp)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedByIp = createdByIp;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedById { get; private set; }
    public string CreatedByIp { get; private set; } = string.Empty;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime nowUtc, string createdByIp) =>
        new(Guid.NewGuid(), userId, tokenHash, nowUtc.AddDays(ExpiryDays), createdByIp);

    public bool IsActive(DateTime nowUtc) => RevokedAt is null && ExpiresAt > nowUtc;

    public bool IsExpired(DateTime nowUtc) => ExpiresAt <= nowUtc;

    /// <summary>Rotation: eski token iptal edilir, zincir yeni token'a işaret eder.</summary>
    public void RevokeAndReplace(DateTime nowUtc, Guid replacedById)
    {
        RevokedAt = nowUtc;
        ReplacedById = replacedById;
    }

    /// <summary>Theft koruması: çalınmış (zaten iptal edilmiş) token tekrar kullanılırsa çağrılır.</summary>
    public void Revoke(DateTime nowUtc)
    {
        RevokedAt = nowUtc;
    }
}
