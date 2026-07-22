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

namespace Campaign.Application.Commands.ManualAssignExpert;

/// <summary>
/// SUPERVIZOR'ün AI atamasını beklemeden/onu geçersiz kılarak elle uzman ataması yapması.
/// CaseStateMachine'de YENI→ATANDI geçişini zaten Supervizor actor'ü de yapabiliyor
/// (§7 tablosu) — bu komut AssignedExpertId'yi set edip AYNI tek mutasyon noktasını kullanır.
/// </summary>
public sealed class ManualAssignExpertCommandHandler : IRequestHandler<ManualAssignExpertCommand, CaseDto>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;

    public ManualAssignExpertCommandHandler(
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

    public async Task<CaseDto> Handle(ManualAssignExpertCommand request, CancellationToken cancellationToken)
    {
        if (_requestContext.UserId is not { } callerId)
        {
            throw new ForbiddenException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");
        }

        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        var rule = CaseStateMachine.FindRule(optimizationCase.Status, CaseStatus.ATANDI);
        if (rule is null || !rule.AllowedActors.Contains(CaseTransitionActor.Supervizor))
        {
            throw new DomainRuleException(
                "INVALID_TRANSITION",
                $"{optimizationCase.Status} durumundaki vakaya manuel atama yapilamaz.");
        }

        var experts = await _identityServiceClient.GetExpertsAsync(cancellationToken);
        var expertsById = experts.ToDictionary(e => e.Id);
        if (!expertsById.ContainsKey(request.ExpertId))
        {
            throw new NotFoundException("EXPERT_NOT_FOUND", "Belirtilen uzman bulunamadi.");
        }

        var now = _dateTimeProvider.UtcNow;
        optimizationCase.AssignedExpertId = request.ExpertId;
        var history = CaseStateMachine.Apply(optimizationCase, CaseStatus.ATANDI, callerId, note: "Manuel atama (SUPERVIZOR)", now);
        _caseRepository.AddStatusHistory(history);

        await _publishEndpoint.PublishIntegrationEventAsync(
            new CaseAssignedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new CaseAssignedPayload(optimizationCase.Id, request.ExpertId, "SUPERVIZOR"),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await CaseDtoAssembler.AssembleAsync(optimizationCase, expertsById, _offerRepository, now, cancellationToken);
    }
}
