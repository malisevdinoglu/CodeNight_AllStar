using Campaign.Domain.Enums;

namespace Campaign.Application.External;

/// <summary>
/// Mali.md §6 AI Service sözleşmesi (Faz 6'da FastAPI tarafında implemente edilecek).
/// Bu tipler Campaign'in AI'dan beklediği REST gövdesidir — Faz 6 bu sözleşmeye uyar.
/// </summary>
public sealed record AiSubscriberProfileDto(
    Guid SubscriberId,
    string CurrentPlan,
    int TenureMonths,
    decimal AvgMonthlyDataGb,
    int AvgMonthlyCallMinutes,
    decimal MonthlySpendTl,
    int PackagePurchaseCount,
    int ComplaintCount,
    int DaysSinceLastActivity,
    decimal PastAcceptanceRate);

public sealed record AiCampaignSummaryDto(
    Guid CampaignId,
    CampaignType Type,
    decimal DiscountRate);

/// <summary>POST /api/v1/ai/recommend girdisi.</summary>
public sealed record AiRecommendRequest(
    AiSubscriberProfileDto SubscriberProfile,
    IReadOnlyList<AiCampaignSummaryDto> Campaigns);

public sealed record AiRecommendationDto(
    Guid CampaignId,
    decimal RecommendationScore,
    decimal ConversionProbability);

/// <summary>POST /api/v1/ai/classify girdisi.</summary>
public sealed record AiClassifyRequest(AiSubscriberProfileDto SubscriberProfile);

public sealed record AiClassifyResult(SegmentType Segment, decimal Confidence);

public sealed record AiCaseSummaryDto(Guid CaseId, SegmentType Segment, CasePriority Priority);

public sealed record AiCandidateDto(
    Guid ExpertId,
    IReadOnlyList<SegmentType> Expertise,
    int ActiveCaseCount,
    decimal PerformanceScore);

/// <summary>POST /api/v1/ai/assign girdisi.</summary>
public sealed record AiAssignRequest(AiCaseSummaryDto Case, IReadOnlyList<AiCandidateDto> Candidates);

public sealed record AiAssignmentScoreDto(Guid ExpertId, decimal Score);
