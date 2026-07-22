using Identity.Domain.Entities;

namespace Identity.Application.Dtos;

public static class UserMappingExtensions
{
    public static UserSummaryDto ToSummaryDto(this User user) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.GsmNumber,
            user.Email,
            user.Role.ToString(),
            user.Region,
            user.Expertises.Select(e => e.SegmentType.ToString()).ToList());

    public static ExpertDto ToExpertDto(this User user) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Region,
            user.Expertises.Select(e => e.SegmentType.ToString()).ToList());

    public static AuditLogDto ToDto(this AuditLog log) =>
        new(
            log.Id,
            log.UserId,
            log.ActionType.ToString(),
            log.OccurredAt,
            log.IpAddress,
            log.Success,
            log.ResourceId,
            log.Details);
}
