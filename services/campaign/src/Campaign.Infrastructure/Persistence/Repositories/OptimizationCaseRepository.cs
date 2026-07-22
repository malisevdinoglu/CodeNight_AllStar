using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence.Repositories;

public sealed class OptimizationCaseRepository : IOptimizationCaseRepository
{
    private static readonly CaseStatus[] TerminalStatuses =
        { CaseStatus.TAMAMLANDI, CaseStatus.YAYINDA, CaseStatus.ARSIVLENDI };

    private readonly CampaignDbContext _dbContext;

    public OptimizationCaseRepository(CampaignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private IQueryable<OptimizationCase> WithCampaign() => _dbContext.OptimizationCases.Include(c => c.Campaign);

    public void Add(OptimizationCase optimizationCase) => _dbContext.OptimizationCases.Add(optimizationCase);

    public void AddStatusHistory(CaseStatusHistory history) => _dbContext.CaseStatusHistory.Add(history);

    public Task<OptimizationCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithCampaign().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<OptimizationCase?> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default) =>
        WithCampaign().FirstOrDefaultAsync(c => c.CampaignId == campaignId, cancellationToken);

    public async Task<(IReadOnlyList<OptimizationCase> Items, int TotalCount)> GetPagedAsync(
        Guid? assignedExpertId,
        CaseStatus? status,
        CasePriority? priority,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = WithCampaign();

        if (assignedExpertId is { } expertId)
        {
            query = query.Where(c => c.AssignedExpertId == expertId);
        }

        if (status is { } s)
        {
            query = query.Where(c => c.Status == s);
        }

        if (priority is { } p)
        {
            query = query.Where(c => c.Priority == p);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<OptimizationCase>> GetAllForDashboardAsync(CancellationToken cancellationToken = default) =>
        await WithCampaign().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OptimizationCase>> GetActiveForSlaSweepAsync(
        DateTimeOffset nowUtc, CancellationToken cancellationToken = default) =>
        await _dbContext.OptimizationCases
            .Where(c => !TerminalStatuses.Contains(c.Status) && !c.SlaBreached && c.SlaDeadline <= nowUtc)
            .ToListAsync(cancellationToken);

    public Task<int> CountActiveByExpertAsync(Guid expertId, CancellationToken cancellationToken = default) =>
        _dbContext.OptimizationCases.CountAsync(
            c => c.AssignedExpertId == expertId && !TerminalStatuses.Contains(c.Status),
            cancellationToken);
}
