using Campaign.Domain.Enums;

namespace Campaign.Application.Dtos;

/// <summary>frontend/src/api/types.ts OfferDto ile birebir.</summary>
public sealed record OfferDto(
    Guid Id,
    Guid CampaignId,
    string CampaignNumber,
    string Title,
    CampaignType Type,
    decimal DiscountRate,
    decimal RecommendationScore,
    bool IsPriority,
    OfferStatus Status,
    DateOnly ValidUntil,
    bool CanRate,
    int? MyRating);
