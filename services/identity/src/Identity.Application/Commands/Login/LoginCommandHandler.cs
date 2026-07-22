using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Application.Dtos;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthTokenIssuer _authTokenIssuer;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        AuthTokenIssuer authTokenIssuer,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _authTokenIssuer = authTokenIssuer;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            _auditLogRepository.Add(AuditLog.Create(
                null, AuditActionType.LOGIN_FAILED, now, _requestContext.IpAddress,
                success: false, resourceId: request.Email, details: "USER_NOT_FOUND"));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException("INVALID_CREDENTIALS", "E-posta veya sifre hatali.");
        }

        // Zaten kilitliyse sifreyi HIC kontrol etme — kalan sureyi don (Core_Principles §10).
        if (user.IsCurrentlyLocked(now))
        {
            _auditLogRepository.Add(AuditLog.Create(
                user.Id, AuditActionType.LOGIN_FAILED, now, _requestContext.IpAddress,
                success: false, resourceId: null, details: "ACCOUNT_LOCKED"));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new AccountLockedException(user.RemainingLockSeconds(now));
        }

        var passwordValid = user.PasswordHash is not null && _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            user.RegisterFailedLogin(now);

            var justLocked = user.IsCurrentlyLocked(now);
            _auditLogRepository.Add(AuditLog.Create(
                user.Id,
                justLocked ? AuditActionType.ACCOUNT_LOCKED : AuditActionType.LOGIN_FAILED,
                now, _requestContext.IpAddress, success: false, resourceId: null,
                details: $"failedLoginCount={user.FailedLoginCount}"));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (justLocked)
            {
                throw new AccountLockedException(user.RemainingLockSeconds(now));
            }

            throw new InvalidCredentialsException("INVALID_CREDENTIALS", "E-posta veya sifre hatali.");
        }

        user.ResetFailedLoginCount();

        var issued = _authTokenIssuer.IssueFor(user, _requestContext.IpAddress);

        _auditLogRepository.Add(AuditLog.Create(
            user.Id, AuditActionType.LOGIN_SUCCESS, now, _requestContext.IpAddress,
            success: true, resourceId: null, details: null));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.AuthResult;
    }
}
