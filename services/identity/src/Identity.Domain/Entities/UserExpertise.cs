using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

/// <summary>Iskender.md §1 <c>user_expertises</c> — personelin uzmanlık alanları (N adet).</summary>
public class UserExpertise
{
    protected UserExpertise()
    {
    }

    private UserExpertise(Guid id, Guid userId, SegmentType segmentType)
    {
        Id = id;
        UserId = userId;
        SegmentType = segmentType;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SegmentType SegmentType { get; private set; }

    public static UserExpertise Create(Guid userId, SegmentType segmentType) =>
        new(Guid.NewGuid(), userId, segmentType);
}
