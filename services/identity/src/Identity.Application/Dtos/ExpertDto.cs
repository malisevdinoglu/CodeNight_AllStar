namespace Identity.Application.Dtos;

/// <summary>GetExpertsQuery — internal, AI'nın uzman atama skorlamasında kullandığı Campaign çağrısı.</summary>
public sealed record ExpertDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Region,
    IReadOnlyList<string> Expertise);
