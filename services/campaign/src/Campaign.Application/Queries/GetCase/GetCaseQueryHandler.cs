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
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetCaseQueryHandler(
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

    public async Task<CaseDto> Handle(GetCaseQuery request, CancellationToken cancellationToken)
    {
        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        // IDOR (Core_Principles §10 / case §3.3): CasesController "sahiplik/yetki kontrolu
        // handler'larda" der ve ChangeCaseStatusCommandHandler bunu yazma tarafinda zaten
        // uyguluyordu; okuma tarafinda eksikti - PERSONEL sadece KENDISINE atanmis vakayi
        // gorebilir (GUID tahmin/paylasimiyla baskasinin vakasini gormeyi engeller).
        // SUPERVIZOR her vakayi gorebilir (gozetim rolu).
        if (string.Equals(_requestContext.Role, "PERSONEL", StringComparison.Ordinal)
            && optimizationCase.AssignedExpertId != _requestContext.UserId)
        {
            throw new ForbiddenException(
                "FORBIDDEN_CASE_ACCESS", "Bu vakaya erisim yetkiniz yok.");
        }

        var expertsById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(e => e.Id);
        return await CaseDtoAssembler.AssembleAsync(
            optimizationCase, expertsById, _offerRepository, _dateTimeProvider.UtcNow, cancellationToken);
    }
}
