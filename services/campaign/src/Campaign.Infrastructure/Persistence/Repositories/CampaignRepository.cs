using Campaign.Application.Common;
using Microsoft.EntityFrameworkCore;
using CampaignEntity = Campaign.Domain.Entities.Campaign;

namespace Campaign.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly CampaignDbContext _dbContext;

    public CampaignRepository(CampaignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(CampaignEntity campaign) => _dbContext.Campaigns.Add(campaign);

    public Task<CampaignEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CampaignEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Campaigns.OrderByDescending(c => c.CreatedAt).ToListAsync(cancellationToken);
}
