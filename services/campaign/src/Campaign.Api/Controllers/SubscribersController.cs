using BuildingBlocks.Common;
using Campaign.Application.Queries.GetSubscriberOffers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/subscribers")]
[Authorize]
public sealed class SubscribersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscribersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>IDOR: MUSTERI sadece kendi subscriberId'si icin cagirabilir (handler'da dogrulanir).</summary>
    [HttpGet("{id:guid}/offers")]
    public async Task<IActionResult> GetOffers(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubscriberOffersQuery(id), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}
