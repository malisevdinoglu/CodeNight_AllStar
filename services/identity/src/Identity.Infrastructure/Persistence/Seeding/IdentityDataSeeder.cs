using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo kullanicilarini seed'ler (docs/SEED_DATA.md tablosuyla birebir).
/// Mali'nin zengin domain modelini (factory metotlari) ve IPasswordHasher'ini kullanir
/// → seed'lenen hash, login'in Verify'ladigiyla birebir ayni (ayni work factor).
/// Idempotent: users doluysa hicbir sey yapmaz. Sabit GUID'ler SeedIds'ten gelir
/// (Campaign/Gamification ayni degerleri kullanir — cross-service seed koordinasyonu).
/// </summary>
public sealed class IdentityDataSeeder : IDataSeeder
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public IdentityDataSeeder(IdentityDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(cancellationToken))
            return;

        var staffPassword = _passwordHasher.Hash("Uzman.2026!");

        var users = new List<User>
        {
            User.CreateAdmin("Sistem", "Admin", "admin@campaigncell.com",
                _passwordHasher.Hash("Admin.2026!"), SeedIds.Admin),

            User.CreateStaff("Serkan", "Ünal", "supervizor@campaigncell.com",
                _passwordHasher.Hash("Super.2026!"), Role.SUPERVIZOR, "MARMARA",
                Array.Empty<SegmentType>(), SeedIds.Supervisor),

            // 4 uzman — farkli uzmanlik kombinasyonlari (atama algoritmasi demoda farkli sonuclar versin)
            User.CreateStaff("Deniz", "Karaca", "deniz.karaca@campaigncell.com", staffPassword,
                Role.PERSONEL, "MARMARA", new[] { SegmentType.RISKLI_KAYIP }, SeedIds.ExpertDeniz),
            User.CreateStaff("Merve", "Aksoy", "merve.aksoy@campaigncell.com", staffPassword,
                Role.PERSONEL, "IC_ANADOLU", new[] { SegmentType.YUKSEK_DEGER }, SeedIds.ExpertMerve),
            User.CreateStaff("Kaan", "Erdem", "kaan.erdem@campaigncell.com", staffPassword,
                Role.PERSONEL, "EGE", new[] { SegmentType.YENI_ABONE, SegmentType.PASIF }, SeedIds.ExpertKaan),
            User.CreateStaff("Ece", "Yıldız", "ece.yildiz@campaigncell.com", staffPassword,
                Role.PERSONEL, "AKDENIZ",
                new[] { SegmentType.YUKSEK_DEGER, SegmentType.RISKLI_KAYIP, SegmentType.YENI_ABONE, SegmentType.PASIF },
                SeedIds.ExpertEce),
        };

        // 10 abone — sabit GUID'ler (Campaign subscriber_profiles ile eslesir), OTP 1234 ile girer
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
            var subscriber = User.CreateSubscriber(s.First, s.Last, s.Gsm, email: null, id: SeedIds.Subscriber(i + 1));
            subscriber.Activate(); // demo: seed aboneleri mevcut musteri → dogrudan OTP ile girebilsin
            users.Add(subscriber);
        }

        await _db.Users.AddRangeAsync(users, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
