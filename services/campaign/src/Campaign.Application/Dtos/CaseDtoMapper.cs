using Campaign.Domain.Entities;
using Campaign.Domain.Services;

namespace Campaign.Application.Dtos;

/// <summary>
/// OptimizationCase anemik olduğu için (İskender'in EF şeması) DTO üretimi entity metodu değil,
/// burada tek yerde toplanan bir mapper'dır. Ek veriler (uzman adı, dönüşüm olasılığı) caller'dan gelir
/// çünkü bunlar cross-aggregate/cross-service veriler — Domain/entity bunları bilemez.
/// </summary>
public static class CaseDtoMapper
{
    public static CaseDto ToDto(
        this OptimizationCase optimizationCase,
        DateTimeOffset nowUtc,
        string? assignedExpertName,
        decimal? conversionProbability)
    {
        return new CaseDto(
            optimizationCase.Id,
            optimizationCase.CaseNumber,
            optimizationCase.Campaign?.Title ?? string.Empty,
            optimizationCase.Segment,
            optimizationCase.Priority,
            optimizationCase.Status,
            optimizationCase.AssignedExpertId,
            assignedExpertName,
            conversionProbability,
            SlaPolicy.RemainingSeconds(optimizationCase.SlaDeadline, nowUtc),
            optimizationCase.SlaBreached,
            optimizationCase.ExpertNote,
            optimizationCase.CreatedAt,
            CaseStateMachine.GetAllowedNextStatuses(optimizationCase.Status));
    }
}
