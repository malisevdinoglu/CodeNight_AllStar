using BuildingBlocks.Common;
using BuildingBlocks.Exceptions;
using Gamification.Application.Queries.GetProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamification.Api.Controllers;

[ApiController]
[Route("api/v1/game")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>GET /api/v1/game/me/profile - herkes kendi profilini gorur.</summary>
    [HttpGet("me/profile")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProfileQuery(GetCallerId()), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    /// <summary>GET /api/v1/game/experts/{expertId}/profile - SUPERVIZOR-only, başka bir uzmanın profili.</summary>
    [HttpGet("experts/{expertId:guid}/profile")]
    [Authorize(Roles = "SUPERVIZOR")]
    public async Task<IActionResult> GetExpertProfile(Guid expertId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProfileQuery(expertId), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    private Guid GetCallerId()
    {
        var raw = User.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id)
            ? id
            : throw new ForbiddenException("UNAUTHENTICATED", "Kimlik dogrulanamadi.");
    }
}
