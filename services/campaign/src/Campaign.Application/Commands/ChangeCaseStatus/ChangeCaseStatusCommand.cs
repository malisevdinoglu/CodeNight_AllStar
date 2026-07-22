using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Commands.ChangeCaseStatus;

/// <summary>
/// Core_Principles §7 / Mali.md: TEK giriş noktası — tüm vaka durum geçişleri buradan geçer.
/// frontend/src/api/types.ts CaseStatusRequest ile birebir.
/// </summary>
public sealed record ChangeCaseStatusCommand(
    Guid CaseId,
    CaseStatus TargetStatus,
    string? Note) : IRequest<CaseDto>;
