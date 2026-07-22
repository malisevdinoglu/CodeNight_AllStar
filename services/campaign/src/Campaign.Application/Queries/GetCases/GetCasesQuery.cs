using BuildingBlocks.Common;
using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Queries.GetCases;

/// <summary>frontend CasesQuery (GET /cases) ile birebir.</summary>
public sealed record GetCasesQuery(
    bool? AssignedToMe,
    CaseStatus? Status,
    CasePriority? Priority,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<CaseDto>>;
