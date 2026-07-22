using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    /// <summary>5xxxxxxxxx — sadece abonelerde dolu, personel/admin e-posta ile girer.</summary>
    public string? GsmNumber { get; set; }

    /// <summary>Personel/admin icin zorunlu, abonede opsiyonel.</summary>
    public string? Email { get; set; }

    /// <summary>Abonede NULL (OTP ile girer). bcrypt hash, asla duz metin.</summary>
    public string? PasswordHash { get; set; }

    public UserRole Role { get; set; }
    public string? Region { get; set; }
    public bool IsActive { get; set; } = true;

    public int FailedLoginCount { get; set; }

    /// <summary>5 basarisiz giriste now + 15 dk; dolunca giris tekrar acilir.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserExpertise> Expertises { get; set; } = new List<UserExpertise>();
}
