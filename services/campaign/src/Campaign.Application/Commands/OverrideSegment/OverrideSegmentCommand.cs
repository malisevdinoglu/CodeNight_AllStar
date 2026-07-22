using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Commands.OverrideSegment;

/// <summary>SUPERVIZOR-only. frontend SegmentOverrideRequest ile birebir.</summary>
public sealed record OverrideSegmentCommand(Guid CaseId, SegmentType Segment) : IRequest<CaseDto>;
