using Gamification.Domain.Entities;

namespace Gamification.Application.Common;

/// <summary>ExpertBadge composite PK (ExpertId + BadgeCode) — aynı rozet iki kez kazanılamaz.</summary>
public interface IExpertBadgeRepository
{
    void Add(ExpertBadge expertBadge);

    /// <summary>BadgeEvaluator'a geçirilecek "zaten kazanılmış" kod kümesi.</summary>
    Task<IReadOnlySet<string>> GetEarnedBadgeCodesAsync(
        Guid expertId, CancellationToken cancellationToken = default);

    /// <summary>Profil ekranı için Badge navigasyonuyla birlikte (Name/Description) tüm rozetler.</summary>
    Task<IReadOnlyList<ExpertBadge>> GetByExpertIdWithBadgeAsync(
        Guid expertId, CancellationToken cancellationToken = default);
}
