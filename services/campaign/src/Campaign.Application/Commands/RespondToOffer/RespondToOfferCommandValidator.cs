using Campaign.Domain.Enums;
using FluentValidation;

namespace Campaign.Application.Commands.RespondToOffer;

public sealed class RespondToOfferCommandValidator : AbstractValidator<RespondToOfferCommand>
{
    public RespondToOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Response)
            .Must(r => r is OfferStatus.KABUL or OfferStatus.RET)
            .WithMessage("Yanit KABUL veya RET olmalidir.");
    }
}
