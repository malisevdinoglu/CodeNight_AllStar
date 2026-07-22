using Gamification.Application.Events;
using MediatR;

namespace Gamification.Application.Commands.ProcessOfferRated;

/// <summary>offer.rated consumer'ının MediatR köprüsü.</summary>
public sealed record ProcessOfferRatedCommand(Guid EventId, OfferRatedPayload Payload) : IRequest;
