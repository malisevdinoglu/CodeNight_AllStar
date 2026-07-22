using Campaign.Application.Common;
using Campaign.Application.External;
using Campaign.Domain.Entities;

namespace Campaign.Application.Dtos;

/// <summary>
/// Query handler'ların ortak kullandığı, cross-aggregate (Identity uzman adı + kampanya
/// tekliflerinin ortalaması) CaseDto derleyicisi. Tek vaka ve liste için ayrı overload'lar —
/// liste overload'u Identity'ye TEK bir GetExpertsAsync çağrısı yapar (N+1 önlenir).
/// </summary>
public static class CaseDtoAssembler
{
    public static async Task<CaseDto> AssembleAsync(
        OptimizationCase optimizationCase,
        IReadOnlyDictionary<Guid, IdentityExpertDto> expertsById,
        IOfferRepository offerRepository,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        string? assignedExpertName = null;
        if (optimizationCase.AssignedExpertId is { } expertId && expertsById.TryGetValue(expertId, out var expert))
        {
            assignedExpertName = $"{expert.FirstName} {expert.LastName}";
        }

        var offers = await offerRepository.GetByCampaignIdAsync(optimizationCase.CampaignId, cancellationToken);
        var conversionProbability = offers.Count > 0 ? offers.Average(o => o.ConversionProbability) : (decimal?)null;

        return optimizationCase.ToDto(nowUtc, assignedExpertName, conversionProbability);
    }

    public static async Task<IReadOnlyList<CaseDto>> AssembleManyAsync(
        IEnumerable<OptimizationCase> cases,
        IIdentityServiceClient identityServiceClient,
        IOfferRepository offerRepository,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var expertsById = (await identityServiceClient.GetExpertsAsync(cancellationToken))
            .ToDictionary(e => e.Id);

        var result = new List<CaseDto>();
        foreach (var optimizationCase in cases)
        {
            result.Add(await AssembleAsync(optimizationCase, expertsById, offerRepository, nowUtc, cancellationToken));
        }

        return result;
    }
}
