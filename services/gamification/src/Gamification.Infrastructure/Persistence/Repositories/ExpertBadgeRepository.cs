using Gamification.Application.Common;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence.Repositories;

public sealed class ExpertBadgeRepository : IExpertBadgeRepository
{
    private readonly GamificationDbContext _dbContext;

    public ExpertBadgeRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(ExpertBadge expertBadge) => _dbContext.ExpertBadges.Add(expertBadge);

    public async Task<IReadOnlySet<string>> GetEarnedBadgeCodesAsync(
        Guid expertId, CancellationToken cancellationToken = default)
    {
        var codes = await _dbContext.ExpertBadges
            .Where(b => b.ExpertId == expertId)
            .Select(b => b.BadgeCode)
            .ToListAsync(cancellationToken);

        return codes.ToHashSet();
    }

    public async Task<IReadOnlyList<ExpertBadge>> GetByExpertIdWithBadgeAsync(
        Guid expertId, CancellationToken cancellationToken = default) =>
        await _dbContext.ExpertBadges
            .Include(b => b.Badge)
            .Where(b => b.ExpertId == expertId)
            .OrderByDescending(b => b.EarnedAt)
            .ToListAsync(cancellationToken);
}
