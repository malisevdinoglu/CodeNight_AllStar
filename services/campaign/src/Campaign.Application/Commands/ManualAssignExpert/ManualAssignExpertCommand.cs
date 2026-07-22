using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Commands.ManualAssignExpert;

/// <summary>SUPERVIZOR-only. frontend AssignCaseRequest (POST /cases/:id/assign) ile birebir.</summary>
public sealed record ManualAssignExpertCommand(Guid CaseId, Guid ExpertId) : IRequest<CaseDto>;
