using BuildingBlocks.Exceptions;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.External;
using MediatR;

namespace Campaign.Application.Queries.GetCase;

public sealed class GetCaseQueryHandler : IRequestHandler<GetCaseQuery, CaseDto>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCaseQueryHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        IDateTimeProvider dateTimeProvider)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CaseDto> Handle(GetCaseQuery request, CancellationToken cancellationToken)
    {
        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        var expertsById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(e => e.Id);
        return await CaseDtoAssembler.AssembleAsync(
            optimizationCase, expertsById, _offerRepository, _dateTimeProvider.UtcNow, cancellationToken);
    }
}
