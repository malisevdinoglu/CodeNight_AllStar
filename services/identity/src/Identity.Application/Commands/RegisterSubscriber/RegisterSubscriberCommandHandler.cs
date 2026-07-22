using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Commands.RegisterSubscriber;

public sealed class RegisterSubscriberCommandHandler : IRequestHandler<RegisterSubscriberCommand, RegisterSubscriberResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterSubscriberCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterSubscriberResult> Handle(RegisterSubscriberCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByGsmNumberAsync(request.GsmNumber, cancellationToken))
        {
            throw new ConflictException(
                "GSM_ALREADY_REGISTERED",
                "Bu GSM numarasi ile zaten kayitli bir hesap var.");
        }

        var user = User.CreateSubscriber(request.FirstName, request.LastName, request.GsmNumber, request.Email);

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterSubscriberResult(user.Id, user.GsmNumber!);
    }
}
