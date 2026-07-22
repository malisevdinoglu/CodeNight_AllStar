using Campaign.Domain.Enums;

namespace Campaign.Application.External;

/// <summary>
/// Core_Principles §3 "Graceful degradation": her çağrı 3 sn timeout + try/catch içerir
/// (implementasyon Infrastructure'da). Başarısızlıkta metotlar null döner — Application
/// katmanı null'ı "AI kapalı, BELIRSIZ/ORTA fallback'e geç" sinyali olarak okur; exception
/// FIRLATILMAZ (kampanya akışını asla kesmemesi gerekir — demo adım 7 sigortası).
/// </summary>
public interface IAiServiceClient
{
    Task<IReadOnlyList<AiRecommendationDto>?> RecommendAsync(
        AiRecommendRequest request, CancellationToken cancellationToken = default);

    Task<AiClassifyResult?> ClassifyAsync(
        AiClassifyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiAssignmentScoreDto>?> AssignAsync(
        AiAssignRequest request, CancellationToken cancellationToken = default);

    /// <summary>GET /api/v1/ai/metrics/accuracy — dashboard için; AI kapalıysa null.</summary>
    Task<AiAccuracyMetricsDto?> GetAccuracyMetricsAsync(CancellationToken cancellationToken = default);
}

public sealed record AiAccuracyByCategoryMetricDto(SegmentType Segment, decimal Accuracy, int Total);

public sealed record AiAccuracyMetricsDto(decimal Overall, IReadOnlyList<AiAccuracyByCategoryMetricDto> ByCategory);
