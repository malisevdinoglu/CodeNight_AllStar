using BuildingBlocks.Common;
using Campaign.Application.Commands.ChangeCaseStatus;
using Campaign.Application.Commands.ChangePriority;
using Campaign.Application.Commands.ManualAssignExpert;
using Campaign.Application.Commands.OverrideSegment;
using Campaign.Application.Queries.GetCase;
using Campaign.Application.Queries.GetCases;
using Campaign.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Campaign.Api.Controllers;

/// <summary>PERSONEL/SUPERVIZOR erişimi; sahiplik/yetki kontrolü handler'larda (IDOR/§7).</summary>
[ApiController]
[Route("api/v1/cases")]
[Authorize(Roles = "PERSONEL,SUPERVIZOR")]
public sealed class CasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCases(
        [FromQuery] bool? assignedToMe,
        [FromQuery] CaseStatus? status,
        [FromQuery] CasePriority? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetCasesQuery(assignedToMe, status, priority, page, pageSize), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCase(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCaseQuery(id), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    /// <summary>Core_Principles §7 TEK giriş noktası: tüm durum geçişleri burdan.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id, ChangeCaseStatusRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeCaseStatusCommand(id, body.TargetStatus, body.Note), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    [HttpPatch("{id:guid}/segment")]
    [Authorize(Roles = "SUPERVIZOR")]
    public async Task<IActionResult> OverrideSegment(
        Guid id, SegmentOverrideRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new OverrideSegmentCommand(id, body.Segment), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    [HttpPatch("{id:guid}/priority")]
    [Authorize(Roles = "SUPERVIZOR")]
    public async Task<IActionResult> ChangePriority(
        Guid id, ChangePriorityRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ChangePriorityCommand(id, body.Priority), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "SUPERVIZOR")]
    public async Task<IActionResult> AssignExpert(
        Guid id, AssignCaseRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ManualAssignExpertCommand(id, body.ExpertId), cancellationToken);
        return Ok(ApiResponseFactory.Success(result));
    }
}

public sealed record ChangeCaseStatusRequest(CaseStatus TargetStatus, string? Note);

public sealed record SegmentOverrideRequest(SegmentType Segment);

public sealed record ChangePriorityRequest(CasePriority Priority);

public sealed record AssignCaseRequest(Guid ExpertId);
