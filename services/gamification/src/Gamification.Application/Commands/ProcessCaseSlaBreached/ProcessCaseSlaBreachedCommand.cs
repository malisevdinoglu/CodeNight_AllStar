using Gamification.Application.Events;
using MediatR;

namespace Gamification.Application.Commands.ProcessCaseSlaBreached;

/// <summary>case.sla_breached consumer'ının MediatR köprüsü.</summary>
public sealed record ProcessCaseSlaBreachedCommand(Guid EventId, CaseSlaBreachedPayload Payload) : IRequest;
