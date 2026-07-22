using Gamification.Application.Commands.ProcessOfferRated;
using Gamification.Application.Events;
using MassTransit;
using MediatR;

namespace Gamification.Api.Consumers;

public sealed class OfferRatedConsumer : IConsumer<OfferRatedEvent>
{
    private readonly IMediator _mediator;

    public OfferRatedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<OfferRatedEvent> context)
    {
        await _mediator.Send(
            new ProcessOfferRatedCommand(context.Message.EventId, context.Message.Payload),
            context.CancellationToken);
    }
}
