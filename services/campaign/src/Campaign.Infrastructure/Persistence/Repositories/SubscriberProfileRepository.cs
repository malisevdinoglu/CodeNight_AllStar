using Campaign.Application.Common;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence.Repositories;

public sealed class SubscriberProfileRepository : ISubscriberProfileRepository
{
    private readonly CampaignDbContext _dbContext;

    public SubscriberProfileRepository(CampaignDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SubscriberProfile?> GetByIdAsync(Guid subscriberId, CancellationToken cancellationToken = default) =>
        _dbContext.SubscriberProfiles.FirstOrDefaultAsync(s => s.SubscriberId == subscriberId, cancellationToken);

    public async Task<IReadOnlyList<SubscriberProfile>> GetBySegmentAsync(
        SegmentType segment, CancellationToken cancellationToken = default) =>
        await _dbContext.SubscriberProfiles.Where(s => s.CurrentSegment == segment).ToListAsync(cancellationToken);

    /// <summary>
    /// AI yeniden-siniflandirma sonrasi (henuz baglanmamis bir akis) icin: yuklu bir tracked
    /// entity varsa dogrudan gunceller; yoksa yalnizca CurrentSegment kolonunu isaretleyen
    /// minimal bir stub attach eder (diger zorunlu alanlari SELECT etmeden partial UPDATE).
    /// </summary>
    public void UpsertCurrentSegment(Guid subscriberId, SegmentType segment)
    {
        var tracked = _dbContext.ChangeTracker.Entries<SubscriberProfile>()
            .FirstOrDefault(e => e.Entity.SubscriberId == subscriberId);

        if (tracked is not null)
        {
            tracked.Entity.CurrentSegment = segment;
            return;
        }

        var stub = new SubscriberProfile { SubscriberId = subscriberId, CurrentSegment = segment };
        _dbContext.Attach(stub);
        _dbContext.Entry(stub).Property(p => p.CurrentSegment).IsModified = true;
    }
}
