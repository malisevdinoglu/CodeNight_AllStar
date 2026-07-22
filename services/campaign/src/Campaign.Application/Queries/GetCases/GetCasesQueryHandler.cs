using BuildingBlocks.Common;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.External;
using MediatR;

namespace Campaign.Application.Queries.GetCases;

public sealed class GetCasesQueryHandler : IRequestHandler<GetCasesQuery, PagedResult<CaseDto>>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCasesQueryHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<CaseDto>> Handle(GetCasesQuery request, CancellationToken cancellationToken)
    {
        // assignedToMe: PERSONEL "sadece bana atananlar" filtresi olarak kullanir; SUPERVIZOR icin
        // de gecerli bir filtredir (nadiren atanmis olabilir) - rol bazli kisitlama Api katmaninda.
        Guid? assignedExpertId = request.AssignedToMe == true ? _requestContext.UserId : null;

        var (items, totalCount) = await _caseRepository.GetPagedAsync(
            assignedExpertId, request.Status, request.Priority, request.Page, request.PageSize, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var dtos = await CaseDtoAssembler.AssembleManyAsync(items, _identityServiceClient, _offerRepository, now, cancellationToken);

        return new PagedResult<CaseDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}
