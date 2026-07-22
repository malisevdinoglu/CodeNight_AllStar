using BuildingBlocks.Messaging;
using Campaign.Application.Common;
using Campaign.Application.Events;
using Campaign.Application.External;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Campaign.Application.Commands.AssignExpert;

public sealed class AssignExpertCommandHandler : IRequestHandler<AssignExpertCommand, bool>
{
    /// <summary>Uzman başına aktif (bitmemiş) vaka üst sınırı — dokümante edilmiş pragmatik değer.</summary>
    private const int MaxActiveCasesPerExpert = 5;

    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AssignExpertCommandHandler> _logger;

    public AssignExpertCommandHandler(
        IOptimizationCaseRepository caseRepository,
        IIdentityServiceClient identityServiceClient,
        IAiServiceClient aiServiceClient,
        IUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        IDateTimeProvider dateTimeProvider,
        ILogger<AssignExpertCommandHandler> logger)
    {
        _caseRepository = caseRepository;
        _identityServiceClient = identityServiceClient;
        _aiServiceClient = aiServiceClient;
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<bool> Handle(AssignExpertCommand request, CancellationToken cancellationToken)
    {
        var optimizationCase = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
        if (optimizationCase is null || optimizationCase.Status != CaseStatus.YENI)
        {
            return false;
        }

        // BELIRSIZ segment = AI zaten kapaliydi; uzmanlik eslesmesi anlamsiz, dogrudan manuel kuyruk.
        if (optimizationCase.Segment == SegmentType.BELIRSIZ)
        {
            _logger.LogInformation(
                "Segment BELIRSIZ oldugu icin otomatik atama atlaniyor, vaka manuel kuyrukta. CaseId={CaseId}",
                optimizationCase.Id);
            return false;
        }

        var allExperts = await _identityServiceClient.GetExpertsAsync(cancellationToken);
        var matchingExperts = allExperts.Where(e => e.Expertise.Contains(optimizationCase.Segment)).ToList();
        if (matchingExperts.Count == 0)
        {
            _logger.LogWarning(
                "Segment icin uygun uzman bulunamadi, vaka manuel kuyrukta. CaseId={CaseId} Segment={Segment}",
                optimizationCase.Id, optimizationCase.Segment);
            return false;
        }

        var candidates = new List<(IdentityExpertDto Expert, int ActiveCaseCount)>();
        foreach (var expert in matchingExperts)
        {
            var activeCount = await _caseRepository.CountActiveByExpertAsync(expert.Id, cancellationToken);
            candidates.Add((expert, activeCount));
        }

        var candidatesWithCapacity = candidates.Where(c => c.ActiveCaseCount < MaxActiveCasesPerExpert).ToList();
        if (candidatesWithCapacity.Count == 0)
        {
            _logger.LogInformation(
                "Uygun uzmanlarin tumu kapasite dolu, vaka manuel kuyrukta. CaseId={CaseId}", optimizationCase.Id);
            return false;
        }

        // PerformanceScore: Identity'nin uzman performans metrigi henuz yok; kapasite tersinden
        // turetilen dokumante edilmis basit sinyal (bos kapasitesi fazla olan daha yuksek skor alir).
        // Nihai siralama AI /ai/assign tarafinda yapilir - bu sadece bir girdi ozelligidir.
        var aiCandidates = candidatesWithCapacity
            .Select(c => new AiCandidateDto(
                c.Expert.Id,
                c.Expert.Expertise,
                c.ActiveCaseCount,
                PerformanceScore: 1m - Math.Min(1m, c.ActiveCaseCount / (decimal)MaxActiveCasesPerExpert)))
            .ToList();

        IReadOnlyList<AiAssignmentScoreDto>? scores;
        try
        {
            scores = await _aiServiceClient.AssignAsync(
                new AiAssignRequest(
                    new AiCaseSummaryDto(optimizationCase.Id, optimizationCase.Segment, optimizationCase.Priority),
                    aiCandidates),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            scores = null;
            _logger.LogWarning(ex, "AI /ai/assign cagrisinda beklenmeyen hata. CaseId={CaseId}", optimizationCase.Id);
        }

        Guid bestExpertId;
        if (scores is null || scores.Count == 0)
        {
            // AI kapali: kapasitesi en bos uzmana dus (dokumante edilmis fallback, kuyrukta birikmesin).
            _logger.LogWarning(
                "AI atama servisi kullanilamiyor, en musait uzmana dogrudan atama yapiliyor. CaseId={CaseId}",
                optimizationCase.Id);
            bestExpertId = candidatesWithCapacity.OrderBy(c => c.ActiveCaseCount).First().Expert.Id;
        }
        else
        {
            var best = scores
                .Where(s => candidatesWithCapacity.Any(c => c.Expert.Id == s.ExpertId))
                .OrderByDescending(s => s.Score)
                .FirstOrDefault();

            if (best is null)
            {
                return false;
            }

            bestExpertId = best.ExpertId;
        }

        var now = _dateTimeProvider.UtcNow;
        optimizationCase.AssignedExpertId = bestExpertId;
        var history = CaseStateMachine.Apply(
            optimizationCase, CaseStatus.ATANDI, CaseStatusHistory.SystemUserId,
            note: "AI/sistem otomatik atama", now);
        _caseRepository.AddStatusHistory(history);

        await _publishEndpoint.PublishIntegrationEventAsync(
            new CaseAssignedEvent
            {
                Timestamp = now.UtcDateTime,
                Payload = new CaseAssignedPayload(optimizationCase.Id, bestExpertId, "SYSTEM"),
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
