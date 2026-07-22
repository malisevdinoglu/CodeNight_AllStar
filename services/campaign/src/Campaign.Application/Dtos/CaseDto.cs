using Campaign.Domain.Enums;

namespace Campaign.Application.Dtos;

/// <summary>frontend/src/api/types.ts CaseDto ile birebir (REST alanları camelCase).</summary>
public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string CampaignTitle,
    SegmentType Segment,
    CasePriority Priority,
    CaseStatus Status,
    Guid? AssignedExpertId,
    string? AssignedExpertName,
    decimal? ConversionProbability,
    int RemainingSlaSeconds,
    bool SlaBreached,
    string? ExpertNote,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CaseStatus> AllowedTransitions);
