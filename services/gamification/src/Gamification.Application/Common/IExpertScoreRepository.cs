using Gamification.Domain.Entities;

namespace Gamification.Application.Common;

/// <summary>
/// ExpertScore satırı ilk event'te "lazy" oluşur (Identity'ye senkron REST çağrısı yapılmaz —
/// DisplayName ilgili event payload'ından denormalize edilir, event'ler DisplayName taşımıyorsa
/// caller "Uzman" gibi bir yer tutucuyla oluşturur ve İskender'in ileride ekleyeceği
/// expert.registered/profile event'i ile güncellenir).
/// </summary>
public interface IExpertScoreRepository
{
    void Add(ExpertScore expertScore);

    Task<ExpertScore?> GetByExpertIdAsync(Guid expertId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ExpertScore>> GetManyByIdsAsync(
        IReadOnlyCollection<Guid> expertIds, CancellationToken cancellationToken = default);
}
