using BuildingBlocks.Common;
using Campaign.Application.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "SUPERVIZOR")]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}
