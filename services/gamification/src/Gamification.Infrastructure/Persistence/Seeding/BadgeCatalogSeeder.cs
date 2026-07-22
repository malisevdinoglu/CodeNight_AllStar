using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gamification.Infrastructure.Persistence.Seeding;

/// <summary>
/// Badge katalogu (6 sabit satır, BadgeCodes.cs ile birebir) İskender'in genel <see cref="IDataSeeder"/>
/// sözleşmesinin KAPSAMI DIŞINDA tutulur: bu demo/test verisi değil, rozet özelliğinin kendi
/// referans tablosudur — ExpertBadge.BadgeCode → Badge.Code FK'si (DeleteBehavior.Restrict,
/// bkz. ExpertBadgeConfiguration) satırlar burada olmadan HİÇ rozet kazanılamamasına yol açar.
/// Idempotent upsert: İskender ileride kendi seed'inde aynı satırları eklerse (veya
/// NoOpDataSeeder gerçek bir seeder ile değişirse) bu no-op'a düşer, çakışma yaratmaz.
/// </summary>
public static class BadgeCatalogSeeder
{
    private static readonly IReadOnlyList<Badge> Catalog = new[]
    {
        new Badge { Code = BadgeCodes.IlkKampanya, Name = "İlk Kampanya", Description = "İlk optimizasyonu tamamladı." },
        new Badge { Code = BadgeCodes.HizUstasi, Name = "Hız Ustası", Description = "2 saatin altında 10 optimizasyon tamamladı." },
        new Badge { Code = BadgeCodes.DonusumKrali, Name = "Dönüşüm Kralı", Description = "10 kampanyada hedef dönüşümü aştı." },
        new Badge { Code = BadgeCodes.Maratoncu, Name = "Maratoncu", Description = "Bir günde 20 optimizasyon tamamladı." },
        new Badge { Code = BadgeCodes.ChurnAvcisi, Name = "Churn Avcısı", Description = "10 riskli kayıp vakasını kurtardı." },
        new Badge { Code = BadgeCodes.Uzman, Name = "Uzman", Description = "Tek bir segmentte 50 optimizasyon tamamladı." },
    };

    public static async Task SeedAsync(GamificationDbContext dbContext, ILogger logger, CancellationToken cancellationToken = default)
    {
        var existingCodes = await dbContext.Badges.Select(b => b.Code).ToListAsync(cancellationToken);
        var missing = Catalog.Where(b => !existingCodes.Contains(b.Code)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        dbContext.Badges.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Badge katalogu seed edildi. EklenenSayisi={Count}", missing.Count);
    }
}
