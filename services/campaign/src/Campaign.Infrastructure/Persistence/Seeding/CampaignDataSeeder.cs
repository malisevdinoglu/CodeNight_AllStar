using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo kampanya/vaka/profil verisini seed'ler (docs/SEED_DATA.md ile birebir).
/// Idempotent: subscriber_profiles doluysa hicbir sey yapmaz.
/// Vaka zamanlari relative (createdHoursAgo): seed ne zaman calisirsa calissin
/// KRITIK vakanin SLA'si "yaklasmis" gorunur — dashboard hep canli hisseder.
/// </summary>
public sealed class CampaignDataSeeder : IDataSeeder
{
    private readonly CampaignDbContext _db;

    public CampaignDataSeeder(CampaignDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.SubscriberProfiles.AnyAsync(cancellationToken))
            return;

        await SeedProfilesAsync(cancellationToken);
        await SeedCampaignsAndCasesAsync(cancellationToken);
    }

    private async Task SeedProfilesAsync(CancellationToken ct)
    {
        // Sira docs/seed/subscriber_profiles.json ile birebir: 3 YUKSEK_DEGER, 3 RISKLI_KAYIP, 2 YENI_ABONE, 2 PASIF
        var rows = new (string Gsm, string Plan, int Tenure, decimal Data, int Call, decimal Spend,
            int Pkg, int Complaint, int Idle, decimal PastRate, SegmentType Seg)[]
        {
            ("5321104501", "Platinum 60GB", 48, 55.4m, 980, 720.50m, 6, 0, 1, 0.78m, SegmentType.YUKSEK_DEGER),
            ("5335562309", "Red Elite 50GB", 36, 42.1m, 640, 610.00m, 4, 1, 3, 0.66m, SegmentType.YUKSEK_DEGER),
            ("5427718845", "Platinum 40GB", 60, 33.8m, 1210, 495.75m, 3, 0, 2, 0.71m, SegmentType.YUKSEK_DEGER),
            ("5309934417", "GNC 20GB", 30, 6.2m, 95, 210.00m, 0, 5, 41, 0.22m, SegmentType.RISKLI_KAYIP),
            ("5548220196", "Standart 15GB", 22, 4.8m, 130, 185.30m, 1, 4, 35, 0.18m, SegmentType.RISKLI_KAYIP),
            ("5361447083", "Ekonomik 10GB", 55, 8.9m, 210, 240.00m, 0, 6, 28, 0.30m, SegmentType.RISKLI_KAYIP),
            ("5053396728", "Hoşgeldin 25GB", 2, 18.5m, 420, 310.00m, 1, 0, 4, 0.50m, SegmentType.YENI_ABONE),
            ("5442685134", "GNC 20GB", 4, 24.0m, 380, 275.90m, 2, 1, 6, 0.45m, SegmentType.YENI_ABONE),
            ("5317059262", "Ekonomik 5GB", 84, 1.2m, 40, 95.00m, 0, 1, 95, 0.10m, SegmentType.PASIF),
            ("5386671950", "Ekonomik 10GB", 72, 3.4m, 75, 120.00m, 0, 0, 70, 0.14m, SegmentType.PASIF),
        };

        for (var i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            _db.SubscriberProfiles.Add(new SubscriberProfile
            {
                SubscriberId = SeedIds.Subscriber(i + 1),
                GsmNumber = r.Gsm, CurrentPlan = r.Plan, TenureMonths = r.Tenure,
                AvgMonthlyDataGb = r.Data, AvgMonthlyCallMinutes = r.Call, MonthlySpendTl = r.Spend,
                PackagePurchaseCount = r.Pkg, ComplaintCount = r.Complaint,
                DaysSinceLastActivity = r.Idle, PastAcceptanceRate = r.PastRate,
                CurrentSegment = r.Seg
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedCampaignsAndCasesAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        _db.Campaigns.AddRange(
            new CampaignEntity
            {
                Id = SeedIds.Campaign1, CampaignNumber = "CMP-2026-000001",
                Title = "Yaz Fırsatı: 10GB Ek Paket %40 İndirim", Type = CampaignType.EK_PAKET,
                TargetSegment = SegmentType.YUKSEK_DEGER, DiscountRate = 40.0m,
                ValidFrom = today, ValidUntil = today.AddDays(30),
                CreatedBy = SeedIds.Supervisor, CreatedAt = now
            },
            new CampaignEntity
            {
                Id = SeedIds.Campaign2, CampaignNumber = "CMP-2026-000002",
                Title = "Bizi Bırakma: Sadakat Tarifesinde 6 Ay %50", Type = CampaignType.SADAKAT,
                TargetSegment = SegmentType.RISKLI_KAYIP, DiscountRate = 50.0m,
                ValidFrom = today, ValidUntil = today.AddDays(45),
                CreatedBy = SeedIds.Supervisor, CreatedAt = now
            });

        // OPT-000001: KRITIK, 1.5 saat once acildi → SLA 2 saat, deadline'a ~30 dk (dashboard kirmizi)
        _db.OptimizationCases.Add(NewCase("OPT-2026-000001", SeedIds.Campaign2, SegmentType.RISKLI_KAYIP,
            CasePriority.KRITIK, CaseStatus.ATANDI, SeedIds.ExpertDeniz, now.AddHours(-1.5), slaHours: 2));

        // OPT-000002: YUKSEK, aktif calisiliyor
        _db.OptimizationCases.Add(NewCase("OPT-2026-000002", SeedIds.Campaign1, SegmentType.YUKSEK_DEGER,
            CasePriority.YUKSEK, CaseStatus.OPTIMIZE_EDILIYOR, SeedIds.ExpertMerve, now.AddHours(-3), slaHours: 8));

        // OPT-000003: BELIRSIZ + ORTA, atanmamis → AI kapaliyken olusan manuel kuyruk (graceful degradation)
        _db.OptimizationCases.Add(NewCase("OPT-2026-000003", SeedIds.Campaign1, SegmentType.BELIRSIZ,
            CasePriority.ORTA, CaseStatus.YENI, assignedExpertId: null, now.AddMinutes(-30), slaHours: 24));

        await _db.SaveChangesAsync(ct);

        // Seed numaralarindan sonra baslasin ki factory ile catisma olmasin
        await _db.Database.ExecuteSqlRawAsync("SELECT setval('campaign_number_seq', 2, true)", ct);
        await _db.Database.ExecuteSqlRawAsync("SELECT setval('case_number_seq', 3, true)", ct);
    }

    private static OptimizationCase NewCase(string number, Guid campaignId, SegmentType segment,
        CasePriority priority, CaseStatus status, Guid? assignedExpertId, DateTimeOffset createdAt, int slaHours)
        => new()
        {
            CaseNumber = number, CampaignId = campaignId, Segment = segment, Priority = priority,
            Status = status, AssignedExpertId = assignedExpertId, CreatedAt = createdAt,
            SlaDeadline = createdAt.AddHours(slaHours)
        };
}
