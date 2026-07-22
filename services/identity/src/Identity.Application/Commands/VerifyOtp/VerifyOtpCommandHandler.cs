using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Application.Dtos;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.VerifyOtp;

public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, AuthResultDto>
{
    private const string SimulatedOtpCode = "1234";

    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthTokenIssuer _authTokenIssuer;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VerifyOtpCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        AuthTokenIssuer authTokenIssuer,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _authTokenIssuer = authTokenIssuer;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResultDto> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByGsmNumberAsync(request.GsmNumber, cancellationToken);

        // Kullanici bulunamadi ya da kod yanlis — ayni genel mesaj (numara varligi sizdirilmaz).
        if (user is null || request.OtpCode != SimulatedOtpCode)
        {
            _auditLogRepository.Add(AuditLog.Create(
                user?.Id, AuditActionType.LOGIN_FAILED, _dateTimeProvider.UtcNow,
                _requestContext.IpAddress, success: false, resourceId: request.GsmNumber, details: "OTP_INVALID"));
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException("OTP_INVALID", "GSM numarasi veya OTP kodu hatali.");
        }

        user.Activate();

        var issued = _authTokenIssuer.IssueFor(user, _requestContext.IpAddress);

        _auditLogRepository.Add(AuditLog.Create(
            user.Id, AuditActionType.LOGIN_SUCCESS, _dateTimeProvider.UtcNow,
            _requestContext.IpAddress, success: true, resourceId: null, details: "OTP_VERIFIED"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return issued.AuthResult;
    }
}
