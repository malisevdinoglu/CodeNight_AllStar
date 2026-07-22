using Campaign.Domain.Entities;
using Campaign.Domain.Enums;

namespace Campaign.Application.Dtos;

public static class OfferDtoMapper
{
    public static OfferDto ToDto(this Offer offer, int? myRating)
    {
        return new OfferDto(
            offer.Id,
            offer.CampaignId,
            offer.Campaign?.CampaignNumber ?? string.Empty,
            offer.Campaign?.Title ?? string.Empty,
            offer.Campaign?.Type ?? default,
            offer.Campaign?.DiscountRate ?? 0m,
            offer.RecommendationScore,
            offer.IsPriority,
            offer.Status,
            offer.Campaign?.ValidUntil ?? default,
            CanRate: offer.Status == OfferStatus.KABUL && myRating is null,
            myRating);
    }
}
