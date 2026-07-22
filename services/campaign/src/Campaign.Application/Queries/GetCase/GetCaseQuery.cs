using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetCase;

public sealed record GetCaseQuery(Guid CaseId) : IRequest<CaseDto>;
