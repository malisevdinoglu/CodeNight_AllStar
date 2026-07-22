using Campaign.Domain.Enums;

namespace Campaign.Application.External;

public sealed record IdentityExpertDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Region,
    IReadOnlyList<SegmentType> Expertise);

/// <summary>
/// Gateway'i atlayıp container ağından doğrudan Identity'ye konuşur (Core_Principles §6:
/// servisler-arası çağrı X-Internal-Api-Key ile). Identity.Api.UsersController.GetExperts
/// bu çağrının karşılığıdır.
/// </summary>
public interface IIdentityServiceClient
{
    Task<IReadOnlyList<IdentityExpertDto>> GetExpertsAsync(CancellationToken cancellationToken = default);
}
