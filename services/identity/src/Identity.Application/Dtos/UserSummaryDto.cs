namespace Identity.Application.Dtos;

public sealed record UserSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? GsmNumber,
    string? Email,
    string Role,
    string? Region,
    IReadOnlyList<string> Expertise);
