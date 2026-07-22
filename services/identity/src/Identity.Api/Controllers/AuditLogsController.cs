using BuildingBlocks.Common;
using Identity.Application.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

/// <summary>Case §3.4: sadece ADMIN görebilir, sayfalı (Core_Principles §5 sözleşmesi).</summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "ADMIN")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(page, pageSize), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}
