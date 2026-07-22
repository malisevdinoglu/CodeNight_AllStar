using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Queries.GetExperts;

/// <summary>Internal — Campaign servisi uzman atama skorlaması için çağırır (X-Internal-Api-Key).</summary>
public sealed record GetExpertsQuery : IRequest<IReadOnlyList<ExpertDto>>;
