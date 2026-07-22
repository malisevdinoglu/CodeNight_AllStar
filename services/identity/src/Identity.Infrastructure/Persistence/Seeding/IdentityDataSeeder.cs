using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo kullanicilarini seed'ler (docs/SEED_DATA.md tablosuyla birebir).
/// Idempotent: users doluysa hicbir sey yapmaz → iki kez "compose up" duplicate uretmez.
/// Sifreler bcrypt (work factor 11, Core_Principles §10) ile hash'lenir; abonelerde sifre yok (OTP).
/// </summary>
public sealed class IdentityDataSeeder : IDataSeeder
{
    private const int BcryptWorkFactor = 11;
    private readonly IdentityDbContext _db;

    public IdentityDataSeeder(IdentityDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;
        string Hash(string pw) => BCrypt.Net.BCrypt.HashPassword(pw, BcryptWorkFactor);

        var users = new List<User>
        {
            new()
            {
                Id = SeedIds.Admin, FirstName = "Sistem", LastName = "Admin",
                Email = "admin@campaigncell.com", PasswordHash = Hash("Admin.2026!"),
                Role = UserRole.ADMIN, CreatedAt = now
            },
            new()
            {
                Id = SeedIds.Supervisor, FirstName = "Serkan", LastName = "Ünal",
                Email = "supervizor@campaigncell.com", PasswordHash = Hash("Super.2026!"),
                Role = UserRole.SUPERVIZOR, Region = "MARMARA", CreatedAt = now
            },
            Expert(SeedIds.ExpertDeniz, "Deniz", "Karaca", "deniz.karaca@campaigncell.com", "MARMARA",
                now, SegmentType.RISKLI_KAYIP),
            Expert(SeedIds.ExpertMerve, "Merve", "Aksoy", "merve.aksoy@campaigncell.com", "IC_ANADOLU",
                now, SegmentType.YUKSEK_DEGER),
            Expert(SeedIds.ExpertKaan, "Kaan", "Erdem", "kaan.erdem@campaigncell.com", "EGE",
                now, SegmentType.YENI_ABONE, SegmentType.PASIF),
            Expert(SeedIds.ExpertEce, "Ece", "Yıldız", "ece.yildiz@campaigncell.com", "AKDENIZ",
                now, SegmentType.YUKSEK_DEGER, SegmentType.RISKLI_KAYIP, SegmentType.YENI_ABONE, SegmentType.PASIF),
        };

        // 10 abone — GSM'ler docs/seed/identity_users.json ile birebir, sifre yok (OTP 1234)
        var subscribers = new (string First, string Last, string Gsm)[]
        {
            ("Ahmet", "Yılmaz", "5321104501"),
            ("Ayşe", "Demir", "5335562309"),
            ("Mehmet", "Kaya", "5427718845"),
            ("Elif", "Şahin", "5309934417"),
            ("Mustafa", "Çelik", "5548220196"),
            ("Zeynep", "Arslan", "5361447083"),
            ("Emre", "Doğan", "5053396728"),
            ("Fatma", "Koç", "5442685134"),
            ("Burak", "Aydın", "5317059262"),
            ("Selin", "Öztürk", "5386671950"),
        };
        for (var i = 0; i < subscribers.Length; i++)
        {
            var s = subscribers[i];
            users.Add(new User
            {
                Id = SeedIds.Subscriber(i + 1),
                FirstName = s.First, LastName = s.Last, GsmNumber = s.Gsm,
                Role = UserRole.MUSTERI, CreatedAt = now
            });
        }

        await _db.Users.AddRangeAsync(users, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static User Expert(Guid id, string first, string last, string email, string region,
        DateTimeOffset now, params SegmentType[] expertise)
    {
        var user = new User
        {
            Id = id, FirstName = first, LastName = last, Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Uzman.2026!", BcryptWorkFactor),
            Role = UserRole.PERSONEL, Region = region, CreatedAt = now
        };
        foreach (var seg in expertise)
            user.Expertises.Add(new UserExpertise { UserId = id, SegmentType = seg });
        return user;
    }
}
