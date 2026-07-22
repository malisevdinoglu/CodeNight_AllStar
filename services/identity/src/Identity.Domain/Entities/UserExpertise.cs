using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>Personelin uzmanlik alanlari (N adet; akilli atama skorlamasinin girdisi).</summary>
public class UserExpertise
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SegmentType SegmentType { get; set; }

    public User User { get; set; } = null!;
}
