using Identity.Domain.Entities;

namespace Identity.Application.Common;

/// <summary>
/// Salt-okunur sorgular için — yazma işlemleri her zaman <see cref="User"/> agregatı
/// üzerinden yapılır (EF change tracking, aynı DbContext içinde identity map ile tutarlı kalır).
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
