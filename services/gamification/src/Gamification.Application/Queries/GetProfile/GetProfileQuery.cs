using Gamification.Application.Dtos;
using MediatR;

namespace Gamification.Application.Queries.GetProfile;

/// <summary>GET /experts/{expertId}/profile — kendi profili veya (SUPERVIZOR) herhangi biri.</summary>
public sealed record GetProfileQuery(Guid ExpertId) : IRequest<ProfileDto>;
