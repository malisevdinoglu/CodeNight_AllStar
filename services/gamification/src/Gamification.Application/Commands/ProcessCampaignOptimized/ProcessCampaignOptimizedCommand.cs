using Gamification.Application.Events;
using MediatR;

namespace Gamification.Application.Commands.ProcessCampaignOptimized;

/// <summary>campaign.optimized consumer'ının MediatR köprüsü — Api/Consumers katmanı gönderir.</summary>
public sealed record ProcessCampaignOptimizedCommand(Guid EventId, CampaignOptimizedPayload Payload) : IRequest;
