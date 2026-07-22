using Campaign.Application.Common;
using Campaign.Application.Dtos;
using Campaign.Application.External;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Queries.GetDashboardSummary;

/// <summary>
/// Küçük ölçek demo verisi varsayımıyla tüm agregasyonlar bellek üzerinde yapılır
/// (GetAllForDashboardAsync). AI kapalıysa aiAccuracy sıfır/boş döner - dashboard asla
/// AI'ya bağımlı olarak çökmez (Core_Principles §3 graceful degradation).
/// </summary>
public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private static readonly CaseStatus[] CompletedLifecycleStatuses =
        { CaseStatus.TAMAMLANDI, CaseStatus.YAYINDA, CaseStatus.ARSIVLENDI };

    private readonly IOptimizationCaseRepository _caseRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IIdentityServiceClient _identityServiceClient;
    private readonly IAiServiceClient _aiServiceClient;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDashboardSummaryQueryHandler(
        IOptimizationCaseRepository caseRepository,
        IOfferRepository offerRepository,
        IIdentityServiceClient identityServiceClient,
        IAiServiceClient aiServiceClient,
        IDateTimeProvider dateTimeProvider)
    {
        _caseRepository = caseRepository;
        _offerRepository = offerRepository;
        _identityServiceClient = identityServiceClient;
        _aiServiceClient = aiServiceClient;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var allCases = await _caseRepository.GetAllForDashboardAsync(cancellationToken);
        var allOffers = await _offerRepository.GetAllForDashboardAsync(cancellationToken);
        var expertsById = (await _identityServiceClient.GetExpertsAsync(cancellationToken)).ToDictionary(e => e.Id);

        var segmentDistribution = allCases
            .GroupBy(c => c.Segment)
            .Select(g => new SegmentDistributionDto(g.Key, g.Count()))
            .OrderBy(d => d.Segment)
            .ToList();

        var conversionTrend = allOffers
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt.UtcDateTime))
            .Select(g => new ConversionTrendPointDto(
                g.Key,
                Math.Round(g.Count(o => o.Status == OfferStatus.KABUL) / (decimal)g.Count(), 4)))
            .OrderBy(p => p.Date)
            .ToList();

        var slaComplianceRate = allCases.Count > 0
            ? Math.Round(1m - allCases.Count(c => c.SlaBreached) / (decimal)allCases.Count, 4)
            : 1m;

        var slaBreachedActiveCases = allCases
            .Where(c => c.SlaBreached && !CompletedLifecycleStatuses.Contains(c.Status))
            .ToList();
        var slaBreachedActiveDtos = await CaseDtoAssembler.AssembleManyAsync(
            slaBreachedActiveCases, _identityServiceClient, _offerRepository, now, cancellationToken);

        var aiAccuracy = await BuildAiAccuracyAsync(cancellationToken);

        var expertPerformance = allCases
            .Where(c => c.AssignedExpertId is not null && CompletedLifecycleStatuses.Contains(c.Status) && c.CompletedAt is not null)
            .GroupBy(c => c.AssignedExpertId!.Value)
            .Select(g =>
            {
                var lifts = g.Where(c => c.ConversionLift is not null).Select(c => c.ConversionLift!.Value).ToList();
                var durations = g.Select(c => (decimal)(c.CompletedAt!.Value - c.CreatedAt).TotalMinutes).ToList();
                var name = expertsById.TryGetValue(g.Key, out var expert) ? $"{expert.FirstName} {expert.LastName}" : "Bilinmeyen";

                return new ExpertPerformanceDto(
                    g.Key,
                    name,
                    g.Count(),
                    lifts.Count > 0 ? Math.Round(lifts.Average(), 2) : 0m,
                    durations.Count > 0 ? Math.Round(durations.Average(), 1) : 0m);
            })
            .OrderByDescending(e => e.CompletedCount)
            .ToList();

        var pendingQueue = allCases.Where(c => c.Status == CaseStatus.YENI).ToList();
        var pendingQueueDtos = await CaseDtoAssembler.AssembleManyAsync(
            pendingQueue, _identityServiceClient, _offerRepository, now, cancellationToken);

        return new DashboardSummaryDto(
            segmentDistribution,
            conversionTrend,
            slaComplianceRate,
            slaBreachedActiveDtos,
            aiAccuracy,
            expertPerformance,
            pendingQueueDtos);
    }

    private async Task<AiAccuracyDto> BuildAiAccuracyAsync(CancellationToken cancellationToken)
    {
        AiAccuracyMetricsDto? metrics;
        try
        {
            metrics = await _aiServiceClient.GetAccuracyMetricsAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            metrics = null;
        }

        if (metrics is null)
        {
            return new AiAccuracyDto(0m, Array.Empty<AiAccuracyByCategoryDto>());
        }

        return new AiAccuracyDto(
            metrics.Overall,
            metrics.ByCategory.Select(c => new AiAccuracyByCategoryDto(c.Segment, c.Accuracy, c.Total)).ToList());
    }
}
