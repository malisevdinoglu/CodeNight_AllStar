using Campaign.Domain.Enums;

namespace Campaign.Application.Dtos;

/// <summary>frontend/src/api/types.ts CreateCampaignResult ile birebir.</summary>
public sealed record CreateCampaignResult(
    Guid CampaignId,
    string CampaignNumber,
    Guid CaseId,
    string CaseNumber,
    SegmentType PredictedSegment,
    CasePriority Priority,
    decimal? ConversionProbability,
    bool AiAvailable);
