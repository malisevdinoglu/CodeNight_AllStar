using Gamification.Application.Common;
using Gamification.Domain.Constants;
using Gamification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamification.Infrastructure.Persistence.Repositories;

public sealed class PointTransactionRepository : IPointTransactionRepository
{
    private readonly GamificationDbContext _dbContext;

    public PointTransactionRepository(GamificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(PointTransaction transaction) => _dbContext.PointTransactions.Add(transaction);

    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        _dbContext.PointTransactions.AnyAsync(t => t.EventId == eventId, cancellationToken);

    public Task<bool> HasSlaBreachForCaseAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        _dbContext.PointTransactions.AnyAsync(
            t => t.CaseId == caseId && t.Reason == PointReasons.SlaAsimi, cancellationToken);

    public Task<int> CountTodayCompletionsAsync(
        Guid expertId, DateTimeOffset dayStart, DateTimeOffset dayEndExclusive, CancellationToken cancellationToken = default) =>
        _dbContext.PointTransactions.CountAsync(
            t => t.ExpertId == expertId
                 && t.Reason == PointReasons.OptimizasyonTamamlandi
                 && t.CreatedAt >= dayStart
                 && t.CreatedAt < dayEndExclusive,
            cancellationToken);
}
