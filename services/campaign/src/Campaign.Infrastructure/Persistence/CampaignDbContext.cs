using Campaign.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// Campaign servisinin DbContext'i (database-per-service: SADECE kendi DB'sine baglanir).
/// Entity konfigurasyonlari bu assembly'deki IEntityTypeConfiguration
/// implementasyonlarindan otomatik yuklenir (Iskender'in alani).
/// </summary>
public class CampaignDbContext : DbContext
{
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options) : base(options)
    {
    }

    public DbSet<SubscriberProfile> SubscriberProfiles => Set<SubscriberProfile>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<OptimizationCase> OptimizationCases => Set<OptimizationCase>();
    public DbSet<CaseStatusHistory> CaseStatusHistory => Set<CaseStatusHistory>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferRating> OfferRatings => Set<OfferRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CMP-2026-000123 / OPT-2026-000045 numaralarinin kaynagi (CampaignNumberFactory kullanir)
        modelBuilder.HasSequence<long>("campaign_number_seq");
        modelBuilder.HasSequence<long>("case_number_seq");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampaignDbContext).Assembly);
    }
}
