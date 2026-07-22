using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence;

/// <summary>
/// Gamification servisinin DbContext'i (database-per-service: SADECE kendi DB'sine baglanir).
/// Entity konfigurasyonlari bu assembly'deki IEntityTypeConfiguration
/// implementasyonlarindan otomatik yuklenir (Iskender'in alani).
/// </summary>
public class GamificationDbContext : DbContext
{
    public GamificationDbContext(DbContextOptions<GamificationDbContext> options) : base(options)
    {
    }

    public DbSet<ExpertScore> ExpertScores => Set<ExpertScore>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<ExpertBadge> ExpertBadges => Set<ExpertBadge>();
    public DbSet<SegmentCompletionCount> SegmentCompletionCounts => Set<SegmentCompletionCount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamificationDbContext).Assembly);
    }
}
