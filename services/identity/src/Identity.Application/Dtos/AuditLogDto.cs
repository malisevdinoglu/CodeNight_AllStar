namespace Identity.Application.Dtos;

public sealed record AuditLogDto(
    long Id,
    Guid? UserId,
    string ActionType,
    DateTime OccurredAt,
    string IpAddress,
    bool Success,
    string? ResourceId,
    string? Details);
