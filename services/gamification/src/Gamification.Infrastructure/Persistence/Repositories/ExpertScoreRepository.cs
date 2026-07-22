using Gamification.Application.Common;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence.Repositories;

public sealed class ExpertScoreRepository : IExpertScoreRepository
{
    private readonly GamificationDbContext _dbContext;

    public ExpertScoreRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(ExpertScore expertScore) => _dbContext.ExpertScores.Add(expertScore);

    public Task<ExpertScore?> GetByExpertIdAsync(Guid expertId, CancellationToken cancellationToken = default) =>
        _dbContext.ExpertScores.FirstOrDefaultAsync(s => s.ExpertId == expertId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, ExpertScore>> GetManyByIdsAsync(
        IReadOnlyCollection<Guid> expertIds, CancellationToken cancellationToken = default)
    {
        if (expertIds.Count == 0)
        {
            return new Dictionary<Guid, ExpertScore>();
        }

        var scores = await _dbContext.ExpertScores
            .Where(s => expertIds.Contains(s.ExpertId))
            .ToListAsync(cancellationToken);

        return scores.ToDictionary(s => s.ExpertId);
    }
}
