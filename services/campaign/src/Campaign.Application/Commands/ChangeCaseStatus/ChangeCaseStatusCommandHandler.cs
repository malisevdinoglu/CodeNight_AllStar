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
using Microsoft.Extensions.Logging;

namespace Campaign.Application.Commands.ChangeCaseStatus;

/// <summary>
/// Core_Principles §7'nin TEK yürütücüsü: CaseStateMachine tablosuna göre geçiş doğrular,
/// rol/sahiplik kontrolü yapar, TAMAMLANDI'da expertNote zorunluluğu + conversion_lift hesabını
/// uygular ve campaign.optimized event'ini yayınlar.
/// </summary>
public sealed class ChangeCaseStatusCommandHandler : IRequestHandler<ChangeCaseStatusCommand, CaseDto>
{
    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ChangeCaseStatusCommandHandler> _logger;

    public ChangeCaseStatusCommandHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<ChangeCaseStatusCommandHandler> logger)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<CaseDto> Handle(ChangeCaseStatusCommand request, CancellationToken cancellationToken)
    {
        if (_requestContext.UserId is not { } callerId)
        {
            throw new ForbiddenException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");
        }

        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
            ?? throw new NotFoundException("CASE_NOT_FOUND", "Vaka bulunamadi.");

        var rule = CaseStateMachine.FindRule(optimizationCase.Status, request.TargetStatus);
        if (rule is null)
        {
            throw new DomainRuleException(
                "INVALID_TRANSITION",
                $"{optimizationCase.Status} durumundan {request.TargetStatus} durumuna gecilemez.");
        }

        var callerActors = ResolveCallerActors(_requestContext.Role, optimizationCase.AssignedExpertId, callerId);
        if (!rule.AllowedActors.Any(callerActors.Contains))
        {
            _logger.LogWarning(
                "AUDIT yetkisiz durum gecisi denemesi. CaseId={CaseId} CallerId={CallerId} Role={Role} From={From} To={To}",
                optimizationCase.Id, callerId, _requestContext.Role, optimizationCase.Status, request.TargetStatus);
            throw new ForbiddenException(
                "FORBIDDEN_TRANSITION", "Bu durum gecisini yapmaya yetkiniz yok.");
        }

        if (rule.RequiresExpertNote && string.IsNullOrWhiteSpace(request.Note))
        {
            throw new DomainRuleException(
                "EXPERT_NOTE_REQUIRED", "TAMAMLANDI durumuna gecis icin uzman notu zorunludur.");
        }

        var now = _dateTimeProvider.UtcNow;
        var history = CaseStateMachine.Apply(optimizationCase, request.TargetStatus, callerId, request.Note, now);
        _caseRepository.AddStatusHistory(history);

        await _publishEndpoint.PublishIntegrationEventAsync(
            new CaseStatusChangedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new CaseStatusChangedPayload(
                    optimizationCase.Id, history.FromStatus.ToString(), history.ToStatus.ToString(), callerId),
            },
            cancellationToken);

        // Tek sorguda hem conversion_lift (yalnizca TAMAMLANDI'da) hem donen DTO'nun
        // conversionProbability alani icin kullanilir - gereksiz cift sorgu onlenir.
        var campaignOffers = await _offerRepository.GetByCampaignIdAsync(optimizationCase.CampaignId, cancellationToken);

        if (request.TargetStatus == CaseStatus.TAMAMLANDI)
        {
            optimizationCase.ConversionLift = ConversionLiftCalculator.Calculate(campaignOffers);

            await _publishEndpoint.PublishIntegrationEventAsync(
                new CampaignOptimizedEvent
                {
                    Timestamp = now.UtcDateTime,
                    Payload = new CampaignOptimizedPayload(
                        optimizationCase.Id,
                        optimizationCase.AssignedExpertId ?? Guid.Empty,
                        optimizationCase.Segment.ToString(),
                        optimizationCase.Priority.ToString(),
                        optimizationCase.ConversionLift,
                        optimizationCase.CreatedAt,
                        optimizationCase.CompletedAt ?? now),
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string? assignedExpertName = null;
        if (optimizationCase.AssignedExpertId is { } expertId)
        {
            var experts = await _identityServiceClient.GetExpertsAsync(cancellationToken);
            var expert = experts.FirstOrDefault(e => e.Id == expertId);
            assignedExpertName = expert is null ? null : $"{expert.FirstName} {expert.LastName}";
        }

        var conversionProbability = campaignOffers.Count > 0
            ? campaignOffers.Average(o => o.ConversionProbability)
            : (decimal?)null;

        return optimizationCase.ToDto(now, assignedExpertName, conversionProbability);
    }

    /// <summary>
    /// Rol TEK BAŞINA yeterli değildir (IDOR riski): PERSONEL sadece KENDİSİNE atanmış vakada
    /// AssignedExpert olarak sayılır. SUPERVIZOR rolü her vakada Supervizor olarak sayılır.
    /// System actor'ü HTTP çağıranlar için asla döndürülmez (yalnızca AssignExpert gibi
    /// sistem-içi tetikleyiciler kullanır).
    /// </summary>
    private static List<CaseTransitionActor> ResolveCallerActors(string? role, Guid? assignedExpertId, Guid callerId)
    {
        var actors = new List<CaseTransitionActor>();

        if (string.Equals(role, "SUPERVIZOR", StringComparison.Ordinal))
        {
            actors.Add(CaseTransitionActor.Supervizor);
        }

        if (string.Equals(role, "PERSONEL", StringComparison.Ordinal) && assignedExpertId == callerId)
        {
            actors.Add(CaseTransitionActor.AssignedExpert);
        }

        return actors;
    }
}
