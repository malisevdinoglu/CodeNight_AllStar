using FluentValidation;

namespace Campaign.Application.Commands.OverrideSegment;

public sealed class OverrideSegmentCommandValidator : AbstractValidator<OverrideSegmentCommand>
{
    public OverrideSegmentCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Segment).IsInEnum().WithMessage("Gecersiz segment.");
    }
}
