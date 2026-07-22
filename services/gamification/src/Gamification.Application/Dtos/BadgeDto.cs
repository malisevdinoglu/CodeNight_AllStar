namespace Gamification.Application.Dtos;

public sealed record BadgeDto(string Code, string Name, string Description, DateTimeOffset EarnedAt);
