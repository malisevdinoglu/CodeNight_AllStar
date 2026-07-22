using Gamification.Application.Common;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence.Repositories;

public sealed class SegmentCompletionCountRepository : ISegmentCompletionCountRepository
{
    private readonly GamificationDbContext _dbContext;

    public SegmentCompletionCountRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(SegmentCompletionCount row) => _dbContext.SegmentCompletionCounts.Add(row);

    public Task<SegmentCompletionCount?> GetAsync(
        Guid expertId, string segment, CancellationToken cancellationToken = default) =>
        _dbContext.SegmentCompletionCounts
            .FirstOrDefaultAsync(c => c.ExpertId == expertId && c.Segment == segment, cancellationToken);
}
