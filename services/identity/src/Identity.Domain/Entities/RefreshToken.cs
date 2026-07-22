namespace Identity.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash — duz token ASLA saklanmaz.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Rotation zinciri: gecersiz kilinmis token tekrar kullanilirsa bu zincir uzerinden
    /// theft tespiti yapilir ve kullanicinin tum oturumlari kapatilir.
    /// </summary>
    public Guid? ReplacedById { get; set; }

    public string? CreatedByIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public RefreshToken? ReplacedBy { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt is null && !IsExpired;
}
