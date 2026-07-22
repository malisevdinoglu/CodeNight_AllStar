using Campaign.Domain.Entities;
using Campaign.Domain.Enums;

namespace Campaign.Application.Common;

public interface IOptimizationCaseRepository
{
    void Add(OptimizationCase optimizationCase);

    void AddStatusHistory(CaseStatusHistory history);

    /// <summary>Campaign navigasyonu dahil (CaseDto.campaignTitle için).</summary>
    Task<OptimizationCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Bir kampanyanın (1:1) vakası — RateOfferCommand'in ilgili uzmanı bulması için.</summary>
    Task<OptimizationCase?> GetByCampaignIdAsync(Guid campaignId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OptimizationCase> Items, int TotalCount)> GetPagedAsync(
        Guid? assignedExpertId,
        CaseStatus? status,
        CasePriority? priority,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Küçük ölçek demo verisi varsayımıyla dashboard agregasyonu için tam liste.</summary>
    Task<IReadOnlyList<OptimizationCase>> GetAllForDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>SLA worker: TAMAMLANDI/YAYINDA/ARSIVLENDI dışında, deadline geçmiş, henüz işaretlenmemiş.</summary>
    Task<IReadOnlyList<OptimizationCase>> GetActiveForSlaSweepAsync(
        DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    /// <summary>AssignExpertCommand'in kapasite (boşluk oranı) hesaplaması için: uzmanın aktif vaka sayısı.</summary>
    Task<int> CountActiveByExpertAsync(Guid expertId, CancellationToken cancellationToken = default);
}
