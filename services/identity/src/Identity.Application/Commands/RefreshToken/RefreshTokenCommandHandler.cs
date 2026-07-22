using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Application.Dtos;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly AuthTokenIssuer _authTokenIssuer;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ITokenHasher tokenHasher,
        AuthTokenIssuer authTokenIssuer,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _authTokenIssuer = authTokenIssuer;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = _tokenHasher.Sha256(request.RefreshToken);

        var tokenRow = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (tokenRow is null)
        {
            throw new InvalidCredentialsException("REFRESH_TOKEN_INVALID", "Gecersiz refresh token.");
        }

        var user = await _userRepository.GetByIdAsync(tokenRow.UserId, cancellationToken)
                   ?? throw new NotFoundException("USER_NOT_FOUND", "Kullanici bulunamadi.");

        var token = user.FindRefreshToken(tokenRow.Id) ?? tokenRow;

        // Theft koruması: revoke edilmiş bir token tekrar kullanılmaya çalışıldı →
        // kullanıcının TÜM oturumları kapatılır (Core_Principles §10).
        if (token.RevokedAt is not null)
        {
            user.RevokeAllActiveRefreshTokens(now);

            _auditLogRepository.Add(AuditLog.Create(
                user.Id, AuditActionType.TOKEN_THEFT_DETECTED, now, _requestContext.IpAddress,
                success: false, resourceId: token.Id.ToString(), details: "REFRESH_TOKEN_REUSED"));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException(
                "REFRESH_TOKEN_REUSED",
                "Bu token daha once kullanilmis. Guvenlik nedeniyle tum oturumlariniz kapatildi.");
        }

        if (token.IsExpired(now))
        {
            throw new InvalidCredentialsException("REFRESH_TOKEN_EXPIRED", "Refresh token suresi dolmus.");
        }

        // Rotation: yeni token uret, eskisini iptal edip zincire bagla.
        var issued = _authTokenIssuer.IssueFor(user, _requestContext.IpAddress);
        token.RevokeAndReplace(now, issued.RefreshTokenEntity.Id);

        _auditLogRepository.Add(AuditLog.Create(
            user.Id, AuditActionType.TOKEN_REFRESHED, now, _requestContext.IpAddress,
            success: true, resourceId: token.Id.ToString(), details: null));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.AuthResult;
    }
}
