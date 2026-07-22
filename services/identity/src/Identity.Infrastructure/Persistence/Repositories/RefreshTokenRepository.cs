using Identity.Application.Common;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Salt-okunur sorgu: token hash'inden RefreshToken'ı (tracked) getirir. Handler bu entity'yi
/// veya aynı DbContext'in identity map'i sayesinde User agregatı üzerinden gelen aynı tracked
/// instance'ı kullanarak mutasyon yapar — yazma her zaman aggregate metotlarıyla yürür.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _dbContext;

    public RefreshTokenRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
}
