using Identity.Domain.Entities;

namespace Identity.Application.Common;

/// <summary>
/// <see cref="User.GetByIdAsync"/> ve <see cref="GetByEmailAsync"/>/<see cref="GetByGsmNumberAsync"/>
/// her zaman Expertises + RefreshTokens'ı da yükler (agregat küçük, tek sorguda getirmek basitlik sağlar).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByGsmNumberAsync(string gsmNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByGsmNumberAsync(string gsmNumber, CancellationToken cancellationToken = default);

    /// <summary>GetExpertsQuery (internal, Campaign çağırır) — aktif PERSONEL kullanıcıları.</summary>
    Task<IReadOnlyList<User>> GetActiveExpertsAsync(CancellationToken cancellationToken = default);

    void Add(User user);
}
