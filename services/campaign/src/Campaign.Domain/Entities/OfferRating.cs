namespace Campaign.Domain.Entities;

/// <summary>Abone deneyim puani. OfferId UNIQUE = tek seferlik puanlama garantisi (ikinci deneme 409).</summary>
public class OfferRating
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }
    public Guid SubscriberId { get; set; }

    /// <summary>1-5 yildiz (check constraint ile korunur).</summary>
    public short Stars { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Offer Offer { get; set; } = null!;
}
