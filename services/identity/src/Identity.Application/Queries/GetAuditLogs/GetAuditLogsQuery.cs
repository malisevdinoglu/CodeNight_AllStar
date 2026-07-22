using BuildingBlocks.Common;
using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Queries.GetAuditLogs;

/// <summary>ADMIN, sayfalı — Core_Principles §5 sayfalama sözleşmesi (page/pageSize).</summary>
public sealed record GetAuditLogsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<AuditLogDto>>;
