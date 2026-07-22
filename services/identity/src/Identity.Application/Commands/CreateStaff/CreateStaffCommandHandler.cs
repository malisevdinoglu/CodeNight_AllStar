using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Application.Dtos;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.CreateStaff;

public sealed class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, UserSummaryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateStaffCommandHandler(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<UserSummaryDto> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ConflictException("EMAIL_ALREADY_REGISTERED", "Bu e-posta ile zaten bir hesap var.");
        }

        var role = Enum.Parse<Role>(request.Role);
        var expertise = request.Expertise.Select(e => Enum.Parse<SegmentType>(e));
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.CreateStaff(
            request.FirstName, request.LastName, request.Email, passwordHash, role, request.Region, expertise);

        _userRepository.Add(user);

        _auditLogRepository.Add(AuditLog.Create(
            _requestContext.UserId, AuditActionType.STAFF_CREATED, _dateTimeProvider.UtcNow,
            _requestContext.IpAddress, success: true, resourceId: user.Id.ToString(), details: $"role={role}"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToSummaryDto();
    }
}
