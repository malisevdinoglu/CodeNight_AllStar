using BuildingBlocks.Common;
using Campaign.Application.Commands.RateOffer;
using Campaign.Application.Commands.RespondToOffer;
using Campaign.Application.Queries.GetOffer;
using Campaign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/offers")]
[Authorize]
public sealed class OffersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OffersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOffer(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOfferQuery(id), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    /// <summary>MUSTERI-only, IDOR: offer.subscriberId == token.sub (handler'da dogrulanir).</summary>
    [HttpPost("{id:guid}/respond")]
    [Authorize(Roles = "MUSTERI")]
    public async Task<IActionResult> Respond(Guid id, OfferResponseRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RespondToOfferCommand(id, body.Response), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    [HttpPost("{id:guid}/rate")]
    [Authorize(Roles = "MUSTERI")]
    public async Task<IActionResult> Rate(Guid id, OfferRateRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RateOfferCommand(id, body.Stars), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}

public sealed record OfferResponseRequest(OfferStatus Response);

public sealed record OfferRateRequest(int Stars);
