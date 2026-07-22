using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>
/// Iskender.md §1 <c>users</c> tablosuyla birebir. Domain kuralları (kilitleme, aktivasyon)
/// burada yaşar — Application katmanı sadece bu metotları çağırır (anemik model değil).
/// </summary>
public class User
{
    public const int MaxFailedLoginAttempts = 5;
    public const int LockDurationMinutes = 15;

    private readonly List<UserExpertise> _expertises = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    // EF Core icin
    protected User()
    {
    }

    private User(
        Guid id, string firstName, string lastName, Role role,
        string? gsmNumber, string? email, string? passwordHash, string? region, bool isActive)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        GsmNumber = gsmNumber;
        Email = email;
        PasswordHash = passwordHash;
        Region = region;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? GsmNumber { get; private set; }
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public string? Region { get; private set; }
    public bool IsActive { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<UserExpertise> Expertises => _expertises.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>Abone kaydı: GSM + OTP ile aktive edilene kadar pasif (case §3.1).
    /// <paramref name="id"/> yalnızca deterministik seed için verilir (docs/SEED_DATA.md sabit GUID
    /// sözleşmesi); normal kayıtta null → yeni GUID üretilir.</summary>
    public static User CreateSubscriber(string firstName, string lastName, string gsmNumber, string? email, Guid? id = null)
    {
        return new User(
            id ?? Guid.NewGuid(), firstName, lastName, Role.MUSTERI,
            gsmNumber, email, passwordHash: null, region: null, isActive: false);
    }

    /// <summary>Admin tarafından oluşturulan personel/süpervizör (case §3.1) — e-posta+şifre ile hemen aktif.
    /// <paramref name="id"/> yalnızca deterministik seed içindir (bkz. CreateSubscriber).</summary>
    public static User CreateStaff(
        string firstName, string lastName, string email, string passwordHash,
        Role role, string region, IEnumerable<SegmentType> expertise, Guid? id = null)
    {
        if (role is not (Role.PERSONEL or Role.SUPERVIZOR))
        {
            throw new ArgumentException("CreateStaff sadece PERSONEL veya SUPERVIZOR icin kullanilir.", nameof(role));
        }

        var user = new User(
            id ?? Guid.NewGuid(), firstName, lastName, role,
            gsmNumber: null, email, passwordHash, region, isActive: true);

        foreach (var segment in expertise)
        {
            user.AddExpertise(segment);
        }

        return user;
    }

    /// <summary>Seed için admin kullanıcısı. <paramref name="id"/> deterministik seed içindir (bkz. CreateSubscriber).</summary>
    public static User CreateAdmin(string firstName, string lastName, string email, string passwordHash, Guid? id = null)
    {
        return new User(
            id ?? Guid.NewGuid(), firstName, lastName, Role.ADMIN,
            gsmNumber: null, email, passwordHash, region: null, isActive: true);
    }

    public void AddExpertise(SegmentType segmentType)
    {
        if (_expertises.Any(e => e.SegmentType == segmentType))
        {
            return;
        }

        _expertises.Add(UserExpertise.Create(Id, segmentType));
    }

    /// <summary>OTP doğrulandığında (case §3.1: sabit kod "1234" simülasyonu).</summary>
    public void Activate() => IsActive = true;

    public bool IsCurrentlyLocked(DateTime nowUtc) => LockedUntil.HasValue && LockedUntil.Value > nowUtc;

    public int RemainingLockSeconds(DateTime nowUtc) =>
        IsCurrentlyLocked(nowUtc) ? (int)Math.Ceiling((LockedUntil!.Value - nowUtc).TotalSeconds) : 0;

    /// <summary>Başarısız giriş sayacı; 5'te kilitle (Core_Principles §10).</summary>
    public void RegisterFailedLogin(DateTime nowUtc)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLoginAttempts)
        {
            LockedUntil = nowUtc.AddMinutes(LockDurationMinutes);
        }
    }

    public void ResetFailedLoginCount()
    {
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime nowUtc, string createdByIp)
    {
        var token = RefreshToken.Issue(Id, tokenHash, nowUtc, createdByIp);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>Theft koruması: çalınmış (revoke edilmiş) bir refresh token tekrar kullanılırsa
    /// kullanıcının TÜM aktif oturumları kapatılır (Core_Principles §10).</summary>
    public void RevokeAllActiveRefreshTokens(DateTime nowUtc)
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive(nowUtc)))
        {
            token.Revoke(nowUtc);
        }
    }

    public RefreshToken? FindRefreshToken(Guid tokenId) =>
        _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
}
