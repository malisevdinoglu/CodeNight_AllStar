namespace Campaign.Infrastructure.Persistence.Seeding;

/// <summary>
/// Deterministik seed GUID'leri — SERVISLER ARASI ORTAK SOZLESME (docs/SEED_DATA.md).
/// Identity'deki SeedIds ile birebir ayni degerler: subscriber_profiles ve optimization_cases,
/// Identity'nin urettigi abone/uzman GUID'lerine sabit deger uzerinden baglanir (FK degil, cross-service).
/// Deger degisirse Identity ve Gamification seeder'lari da guncellenir.
/// </summary>
public static class SeedIds
{
    // Uzmanlar (Identity ile ayni)
    public static readonly Guid ExpertDeniz = new("e0000000-0000-0000-0000-000000000001");
    public static readonly Guid ExpertMerve = new("e0000000-0000-0000-0000-000000000002");

    // Aboneler (Identity ile ayni; sira docs/seed ile birebir)
    public static Guid Subscriber(int oneBasedIndex) =>
        new($"b0000000-0000-0000-0000-{oneBasedIndex:D12}");

    // Kampanyalar (sadece Campaign'e ait)
    public static readonly Guid Campaign1 = new("ca000000-0000-0000-0000-000000000001");
    public static readonly Guid Campaign2 = new("ca000000-0000-0000-0000-000000000002");

    public static readonly Guid Supervisor = new("50000000-0000-0000-0000-000000000001");
}
