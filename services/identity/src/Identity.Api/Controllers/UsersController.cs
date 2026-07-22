using BuildingBlocks.Common;
using Identity.Api.Security;
using Identity.Application.Commands.CreateStaff;
using Identity.Application.Queries.GetExperts;
using Identity.Application.Queries.GetMe;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMeQuery(), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    /// <summary>Case §3.1: sadece ADMIN personel/süpervizör oluşturabilir.</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateStaff(CreateStaffCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(result));
    }

    /// <summary>
    /// Internal — Campaign servisi uzman atama skorlaması için çağırır. Gateway'i atlayıp
    /// container ağından doğrudan gelir; JWT değil X-Internal-Api-Key ile korunur.
    /// </summary>
    [HttpGet("experts")]
    [AllowAnonymous]
    [ServiceFilter(typeof(InternalApiKeyFilter))]
    public async Task<IActionResult> GetExperts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExpertsQuery(), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}
