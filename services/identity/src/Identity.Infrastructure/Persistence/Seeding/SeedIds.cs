namespace Identity.Infrastructure.Persistence.Seeding;

/// <summary>
/// Deterministik seed GUID'leri — SERVISLER ARASI ORTAK SOZLESME (docs/SEED_DATA.md).
/// Database-per-service'te servisler birbirinin ID'sini runtime'da soramaz; bu yuzden
/// demo verisi sabit GUID'lerle onceden koordine edilir: Identity buradaki abone/uzman
/// GUID'lerini uretir, Campaign (subscriber_profiles) ve Gamification (expert_scores)
/// AYNI sabitleri kullanir. Deger degisirse UC seeder birden guncellenir.
/// </summary>
public static class SeedIds
{
    public static readonly Guid Admin = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Supervisor = new("50000000-0000-0000-0000-000000000001");

    // Uzmanlar (Gamification expert_scores ile ayni)
    public static readonly Guid ExpertDeniz = new("e0000000-0000-0000-0000-000000000001"); // RISKLI_KAYIP
    public static readonly Guid ExpertMerve = new("e0000000-0000-0000-0000-000000000002"); // YUKSEK_DEGER
    public static readonly Guid ExpertKaan = new("e0000000-0000-0000-0000-000000000003");  // YENI_ABONE + PASIF
    public static readonly Guid ExpertEce = new("e0000000-0000-0000-0000-000000000004");   // tum segmentler

    // Aboneler (Campaign subscriber_profiles ile ayni; sira docs/seed ile birebir)
    public static Guid Subscriber(int oneBasedIndex) =>
        new($"b0000000-0000-0000-0000-{oneBasedIndex:D12}");
}
