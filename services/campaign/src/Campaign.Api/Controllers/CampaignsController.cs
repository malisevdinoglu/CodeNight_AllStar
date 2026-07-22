using BuildingBlocks.Common;
using Campaign.Application.Commands.CreateCampaign;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

[ApiController]
[Route("api/v1/campaigns")]
[Authorize]
public sealed class CampaignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CampaignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Mali_Plan.md kritik akış: kampanya oluştur → AI /recommend → Offer/case aç.
    /// Yetki (case §3.3, netleştirme sonrası): MUSTERI YAPAMAZ; PERSONEL/SUPERVIZOR/ADMIN yapabilir.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "PERSONEL,SUPERVIZOR,ADMIN")]
    public async Task<IActionResult> Create(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(result));
    }
}
