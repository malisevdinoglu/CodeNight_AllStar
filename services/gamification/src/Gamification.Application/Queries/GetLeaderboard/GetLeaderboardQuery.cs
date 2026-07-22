using Gamification.Application.Common;
using Gamification.Application.Dtos;
using MediatR;

namespace Gamification.Application.Queries.GetLeaderboard;

/// <summary>GET /leaderboard?period=weekly|allTime&amp;count=10 — herkes tarafından okunabilir.</summary>
public sealed record GetLeaderboardQuery(LeaderboardPeriod Period, int Count) : IRequest<IReadOnlyList<LeaderboardEntryDto>>;
