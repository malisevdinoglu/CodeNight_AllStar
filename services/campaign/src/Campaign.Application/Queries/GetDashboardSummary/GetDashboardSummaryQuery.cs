using Campaign.Application.Dtos;
using MediatR;

namespace Campaign.Application.Queries.GetDashboardSummary;

/// <summary>SUPERVIZOR-only. GET /dashboard/summary.</summary>
public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;
