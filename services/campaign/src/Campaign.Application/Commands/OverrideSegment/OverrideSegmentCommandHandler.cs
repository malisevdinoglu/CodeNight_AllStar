using BuildingBlocks.Exceptions;
using BuildingBlocks.Messaging;
using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.Events;
using Campaign.Application.External;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using MassTransit;
using MediatR;

namespace Campaign.Application.Commands.OverrideSegment;

/// <summary>
/// Mali_Plan.md: SUPERVIZOR AI'nin öngördüğü segmenti düzeltir; segment.overridden yayınlanır
/// (AI consumer classification_feedback tablosuna yazar - doğruluk metriği geri beslemesi).
/// RISKLI_KAYIP min. YUKSEK önceliği kuralı yeni segmentte de uygulanır (SlaPolicy).
/// </summary>
public sealed class OverrideSegmentCommandHandler : IRequestHandler<OverrideSegmentCommand, CaseDto>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public OverrideSegmentCommandHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<CaseDto> Handle(OverrideSegmentCommand request, CancellationToken cancellationToken)
    {
        if (_requestContext.UserId is not { } callerId)
        {
            throw new ForbiddenException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");
        }

        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        if (optimizationCase.Status == CaseStatus.ARSIVLENDI)
        {
            throw new DomainRuleException("CASE_ARCHIVED", "Arsivlenmis vakanin segmenti degistirilemez.");
        }

        var predictedSegment = optimizationCase.Segment;
        optimizationCase.Segment = request.Segment;

        var isActive = optimizationCase.Status is not (CaseStatus.TAMAMLANDI or CaseStatus.YAYINDA or CaseStatus.ARSIVLENDI);
        if (isActive)
        {
            var now = _dateTimeProvider.UtcNow;
            var adjustedPriority = SlaPolicy.ApplyMinimumForSegment(optimizationCase.Priority, request.Segment);
            if (adjustedPriority != optimizationCase.Priority)
            {
                optimizationCase.Priority = adjustedPriority;
                optimizationCase.SlaDeadline = SlaPolicy.CalculateDeadline(adjustedPriority, optimizationCase.CreatedAt);
            }
        }

        await _publishEndpoint.PublishIntegrationEventAsync(
            new SegmentOverriddenEvent
            {
                Timestamp = _dateTimeProvider.UtcNow.UtcDateTime,
                Payload = new SegmentOverriddenPayload(
                    optimizationCase.Id, predictedSegment.ToString(), request.Segment.ToString(), callerId),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var expertsById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(e => e.Id);
        return await CaseDtoAssembler.AssembleAsync(
            optimizationCase, expertsById, _offerRepository, _dateTimeProvider.UtcNow, cancellationToken);
    }
}
