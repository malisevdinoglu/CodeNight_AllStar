using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Queries.GetMe;

/// <summary>Kimliği ICurrentRequestContext'ten (JWT'den) gelir — parametre yok.</summary>
public sealed record GetMeQuery : IRequest<UserSummaryDto>;
