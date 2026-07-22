using Identity.Application.Common;
using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Queries.GetExperts;

public sealed class GetExpertsQueryHandler : IRequestHandler<GetExpertsQuery, IReadOnlyList<ExpertDto>>
{
    private readonly IUserRepository _userRepository;

    public GetExpertsQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<ExpertDto>> Handle(GetExpertsQuery request, CancellationToken cancellationToken)
    {
        var experts = await _userRepository.GetActiveExpertsAsync(cancellationToken);
        return experts.Select(e => e.ToExpertDto()).ToList();
    }
}
