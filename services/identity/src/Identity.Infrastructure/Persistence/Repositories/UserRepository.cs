using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Iki kardes collection Include (Expertises + RefreshTokens) tek sorguda (SingleQuery)
    // calisirsa SQL tarafinda cartesian product olusur (EF'in kendi uyarisi: "loading related
    // collections for more than one collection navigation"). Bu durumda LEFT JOIN satirlari
    // capraz carpilir ve EF'in row-shaping/identity-resolution mantigi, yeni eklenen bir
    // RefreshToken'i (login sirasinda user.IssueRefreshToken ile Guid.NewGuid() alan YENI bir
    // entity) hatali sekilde "zaten izlenen" bir satirla eslestirip Added yerine Modified olarak
    // isaretleyebiliyor — DbUpdateConcurrencyException (0 rows affected, cunku o id DB'de yok).
    // Canli testte dogrulandi: login her zaman 500 donuyordu. AsSplitQuery() iki collection'i
    // ayri SQL sorgulariyla yukleyerek cartesian product'i ve bu identity-resolution hatasini
    // ortadan kaldirir.
    private IQueryable<User> WithAggregate() =>
        _dbContext.Users
            .Include(u => u.Expertises)
            .Include(u => u.RefreshTokens)
            .AsSplitQuery();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithAggregate().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        WithAggregate().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByGsmNumberAsync(string gsmNumber, CancellationToken cancellationToken = default) =>
        WithAggregate().FirstOrDefaultAsync(u => u.GsmNumber == gsmNumber, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<bool> ExistsByGsmNumberAsync(string gsmNumber, CancellationToken cancellationToken = default) =>
        _dbContext.Users.AnyAsync(u => u.GsmNumber == gsmNumber, cancellationToken);

    public async Task<IReadOnlyList<User>> GetActiveExpertsAsync(CancellationToken cancellationToken = default) =>
        await WithAggregate()
            .Where(u => u.Role == Role.PERSONEL && u.IsActive)
            .ToListAsync(cancellationToken);

    public void Add(User user) => _dbContext.Users.Add(user);
}
