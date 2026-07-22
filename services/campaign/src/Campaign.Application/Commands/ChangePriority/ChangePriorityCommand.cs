using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Commands.ChangePriority;

/// <summary>SUPERVIZOR-only manuel öncelik değişikliği (case dokümanında ayrı bir event tanımlı değil).</summary>
public sealed record ChangePriorityCommand(Guid CaseId, CasePriority Priority) : IRequest<CaseDto>;
