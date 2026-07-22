using BuildingBlocks.Exceptions;
using Identity.Application.Common;
using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Queries.GetMe;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, UserSummaryDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentRequestContext _requestContext;

    public GetMeQueryHandler(IUserRepository userRepository, ICurrentRequestContext requestContext)
    {
        _userRepository = userRepository;
        _requestContext = requestContext;
    }

    public async Task<UserSummaryDto> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var userId = _requestContext.UserId
                      ?? throw new InvalidCredentialsException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("USER_NOT_FOUND", "Kullanici bulunamadi.");

        return user.ToSummaryDto();
    }
}
