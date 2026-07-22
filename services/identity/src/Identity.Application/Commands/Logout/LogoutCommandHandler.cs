using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ITokenHasher tokenHasher,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Sha256(request.RefreshToken);
        var tokenRow = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // Idempotent: token zaten yoksa/iptal edilmisse sessizce basarili say.
        if (tokenRow is null)
        {
            return;
        }

        // Core_Principles §10 IDOR kontrolü: refresh token BASKA bir kullaniciya aitse (ör.
        // calinmis/tahmin edilmis bir token'i baska bir hesaptan giris yapmis saldirgan logout
        // icin gonderirse) sessizce iptal ETME - bu, gercek sahibinin oturumunu saldirganin
        // kontrol edebilecegi bir yan kanal olur. Cagiranin kendi token'i olmali.
        if (_requestContext.UserId != tokenRow.UserId)
        {
            throw new ForbiddenException("FORBIDDEN_TOKEN_ACCESS", "Bu token size ait degil.");
        }

        var now = _dateTimeProvider.UtcNow;

        if (tokenRow.IsActive(now))
        {
            var user = await _userRepository.GetByIdAsync(tokenRow.UserId, cancellationToken);
            var token = user?.FindRefreshToken(tokenRow.Id) ?? tokenRow;
            token.Revoke(now);

            _auditLogRepository.Add(AuditLog.Create(
                tokenRow.UserId, AuditActionType.LOGOUT, now, _requestContext.IpAddress,
                success: true, resourceId: null, details: null));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
