using BuildingBlocks.Exceptions;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.External;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using MediatR;

namespace Campaign.Application.Commands.ChangePriority;

/// <summary>
/// SUPERVIZOR manuel öncelik ataması. RISKLI_KAYIP segment için minimum YUKSEK kuralı burada da
/// uygulanır (SlaPolicy.ApplyMinimumForSegment) — manuel işlem bu domain kuralını atlayamaz.
/// SLA sayacı henüz başladığı andaki CreatedAt üzerinden yeniden hesaplanır (adil karşılaştırma).
/// </summary>
public sealed class ChangePriorityCommandHandler : IRequestHandler<ChangePriorityCommand, CaseDto>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePriorityCommandHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<CaseDto> Handle(ChangePriorityCommand request, CancellationToken cancellationToken)
    {
        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        if (optimizationCase.Status is CaseStatus.TAMAMLANDI or CaseStatus.YAYINDA or CaseStatus.ARSIVLENDI)
        {
            throw new DomainRuleException("CASE_NOT_ACTIVE", "Tamamlanmis/yayindaki/arsivlenmis vakanin onceligi degistirilemez.");
        }

        var effectivePriority = SlaPolicy.ApplyMinimumForSegment(request.Priority, optimizationCase.Segment);
        optimizationCase.Priority = effectivePriority;
        optimizationCase.SlaDeadline = SlaPolicy.CalculateDeadline(effectivePriority, optimizationCase.CreatedAt);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var expertsById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(e => e.Id);
        return await CaseDtoAssembler.AssembleAsync(
            optimizationCase, expertsById, _offerRepository, _dateTimeProvider.UtcNow, cancellationToken);
    }
}
