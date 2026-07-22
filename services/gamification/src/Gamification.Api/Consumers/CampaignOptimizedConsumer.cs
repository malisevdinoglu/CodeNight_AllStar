using Gamification.Application.Commands.ProcessCampaignOptimized;
using Gamification.Application.Events;
using MassTransit;
using MediatR;

namespace Gamification.Api.Consumers;

/// <summary>RabbitMQ -&gt; MediatR köprüsü. İş kuralı burada YOK - tamamen ProcessCampaignOptimizedCommandHandler'da.</summary>
public sealed class CampaignOptimizedConsumer : IConsumer<CampaignOptimizedEvent>
{
    private readonly IMediator _mediator;

    public CampaignOptimizedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<CampaignOptimizedEvent> context)
    {
        await _mediator.Send(
            new ProcessCampaignOptimizedCommand(context.Message.EventId, context.Message.Payload),
            context.CancellationToken);
    }
}
