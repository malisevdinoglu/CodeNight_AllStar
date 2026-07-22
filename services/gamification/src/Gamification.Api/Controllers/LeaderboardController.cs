using BuildingBlocks.Common;
using Gamification.Application.Common;
using Gamification.Application.Queries.GetLeaderboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gamification.Api.Controllers;

/// <summary>GET /api/v1/game/leaderboard?period=weekly|allTime&amp;count=10 - herkes (kimliği doğrulanmış) okuyabilir.</summary>
[ApiController]
[Route("api/v1/game/leaderboard")]
[Authorize]
public sealed class LeaderboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaderboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string period = "weekly", [FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        var parsedPeriod = string.Equals(period, "allTime", StringComparison.OrdinalIgnoreCase)
            ? LeaderboardPeriod.AllTime
            : LeaderboardPeriod.Weekly;
        var boundedCount = Math.Clamp(count, 1, 100);

        var result = await _mediator.Send(new GetLeaderboardQuery(parsedPeriod, boundedCount), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}
