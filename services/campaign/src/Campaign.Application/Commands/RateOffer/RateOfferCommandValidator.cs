using FluentValidation;

namespace Campaign.Application.Commands.RateOffer;

public sealed class RateOfferCommandValidator : AbstractValidator<RateOfferCommand>
{
    public RateOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Stars).InclusiveBetween(1, 5).WithMessage("Puan 1 ile 5 arasinda olmalidir.");
    }
}
