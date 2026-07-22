using Gamification.Application.Commands.ProcessCaseSlaBreached;
using Gamification.Application.Events;
using MassTransit;
using MediatR;

namespace Gamification.Api.Consumers;

public sealed class CaseSlaBreachedConsumer : IConsumer<CaseSlaBreachedEvent>
{
    private readonly IMediator _mediator;

    public CaseSlaBreachedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<CaseSlaBreachedEvent> context)
    {
        await _mediator.Send(
            new ProcessCaseSlaBreachedCommand(context.Message.EventId, context.Message.Payload),
            context.CancellationToken);
    }
}
