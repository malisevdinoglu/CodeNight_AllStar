using Campaign.Domain.Enums;
using FluentValidation;

namespace Campaign.Application.Commands.CreateCampaign;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Baslik zorunludur.").MaximumLength(150);

        RuleFor(x => x.Type).IsInEnum().WithMessage("Gecersiz kampanya turu.");

        RuleFor(x => x.TargetSegment)
            .IsInEnum().WithMessage("Gecersiz hedef segment.")
            .NotEqual(SegmentType.BELIRSIZ)
            .WithMessage("BELIRSIZ, AI siniflandiramadiginda kullanilan bir sistem degeridir; hedef segment olarak secilemez.");

        RuleFor(x => x.Description).MaximumLength(500);
    }
}
