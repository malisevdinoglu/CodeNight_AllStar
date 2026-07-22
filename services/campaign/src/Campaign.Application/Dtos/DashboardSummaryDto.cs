using Campaign.Domain.Enums;

namespace Campaign.Application.Dtos;

/// <summary>frontend/src/api/types.ts DashboardSummaryDto ile birebir. SUPERVIZOR-only.</summary>
public sealed record SegmentDistributionDto(SegmentType Segment, int Count);

public sealed record ConversionTrendPointDto(DateOnly Date, decimal Rate);

public sealed record AiAccuracyByCategoryDto(SegmentType Segment, decimal Accuracy, int Total);

public sealed record AiAccuracyDto(decimal Overall, IReadOnlyList<AiAccuracyByCategoryDto> ByCategory);

public sealed record ExpertPerformanceDto(
    Guid ExpertId, string Name, int CompletedCount, decimal AvgLift, decimal AvgDurationMinutes);

public sealed record DashboardSummaryDto(
    IReadOnlyList<SegmentDistributionDto> SegmentDistribution,
    IReadOnlyList<ConversionTrendPointDto> ConversionTrend,
    decimal SlaComplianceRate,
    IReadOnlyList<CaseDto> SlaBreachedActiveCases,
    AiAccuracyDto AiAccuracy,
    IReadOnlyList<ExpertPerformanceDto> ExpertPerformance,
    IReadOnlyList<CaseDto> PendingQueue);
