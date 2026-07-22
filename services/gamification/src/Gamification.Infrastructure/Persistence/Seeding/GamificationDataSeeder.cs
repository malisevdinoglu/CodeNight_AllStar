using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo puan/rozet verisini seed'ler (docs/SEED_DATA.md ile birebir).
/// Idempotent: badges doluysa hicbir sey yapmaz.
/// ExpertId'ler Identity ile ayni sabit GUID'ler (cross-service, FK degil).
/// Uzmanlar bilincli olarak dort ayri seviyeye dagitildi (Bronz/Gumus/Altin) → tum seviyeler ekranda gorunur.
/// Redis liderlik tablosu bu expert_scores'tan turetilir (servis acilisinda), burada seed'lenmez.
/// </summary>
public sealed class GamificationDataSeeder : IDataSeeder
{
    private static readonly Guid ExpertDeniz = new("e0000000-0000-0000-0000-000000000001");
    private static readonly Guid ExpertMerve = new("e0000000-0000-0000-0000-000000000002");
    private static readonly Guid ExpertKaan = new("e0000000-0000-0000-0000-000000000003");
    private static readonly Guid ExpertEce = new("e0000000-0000-0000-0000-000000000004");

    private readonly GamificationDbContext _db;

    public GamificationDataSeeder(GamificationDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Badges.AnyAsync(cancellationToken))
            return;

        _db.Badges.AddRange(
            new Badge { Code = BadgeCodes.IlkKampanya, Name = "İlk Kampanya", Description = "İlk optimizasyonu tamamlama" },
            new Badge { Code = BadgeCodes.HizUstasi, Name = "Hız Ustası", Description = "2 saatin altında 10 optimizasyon" },
            new Badge { Code = BadgeCodes.DonusumKrali, Name = "Dönüşüm Kralı", Description = "10 kampanyada hedef aşımı" },
            new Badge { Code = BadgeCodes.Maratoncu, Name = "Maratoncu", Description = "Bir günde 20 optimizasyon" },
            new Badge { Code = BadgeCodes.ChurnAvcisi, Name = "Churn Avcısı", Description = "10 RISKLI_KAYIP vakayı kurtarma" },
            new Badge { Code = BadgeCodes.Uzman, Name = "Uzman", Description = "Tek segmentte 50 optimizasyon" });
        await _db.SaveChangesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;

        _db.ExpertScores.AddRange(
            new ExpertScore
            {
                ExpertId = ExpertDeniz, DisplayName = "Deniz Karaca", TotalPoints = 1600, // ALTIN
                CompletedCaseCount = 92, FastCompletionCount = 14, TargetExceededCount = 11,
                RiskliKayipSavedCount = 9, UpdatedAt = now
            },
            new ExpertScore
            {
                ExpertId = ExpertMerve, DisplayName = "Merve Aksoy", TotalPoints = 750, // GUMUS
                CompletedCaseCount = 45, FastCompletionCount = 8, TargetExceededCount = 5,
                RiskliKayipSavedCount = 1, UpdatedAt = now
            },
            new ExpertScore
            {
                ExpertId = ExpertKaan, DisplayName = "Kaan Erdem", TotalPoints = 350, // BRONZ
                CompletedCaseCount = 21, FastCompletionCount = 3, TargetExceededCount = 2,
                RiskliKayipSavedCount = 0, UpdatedAt = now
            },
            new ExpertScore
            {
                ExpertId = ExpertEce, DisplayName = "Ece Yıldız", TotalPoints = 80, // BRONZ (yeni)
                CompletedCaseCount = 6, FastCompletionCount = 1, TargetExceededCount = 0,
                RiskliKayipSavedCount = 2, UpdatedAt = now
            });

        _db.ExpertBadges.AddRange(
            new ExpertBadge { ExpertId = ExpertDeniz, BadgeCode = BadgeCodes.IlkKampanya, EarnedAt = now.AddDays(-30) },
            new ExpertBadge { ExpertId = ExpertDeniz, BadgeCode = BadgeCodes.DonusumKrali, EarnedAt = now.AddDays(-5) },
            new ExpertBadge { ExpertId = ExpertMerve, BadgeCode = BadgeCodes.IlkKampanya, EarnedAt = now.AddDays(-20) },
            new ExpertBadge { ExpertId = ExpertKaan, BadgeCode = BadgeCodes.IlkKampanya, EarnedAt = now.AddDays(-10) });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
