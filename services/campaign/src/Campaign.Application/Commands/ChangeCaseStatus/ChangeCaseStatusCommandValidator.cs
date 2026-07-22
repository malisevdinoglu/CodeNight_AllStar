using FluentValidation;

namespace Campaign.Application.Commands.ChangeCaseStatus;

public sealed class ChangeCaseStatusCommandValidator : AbstractValidator<ChangeCaseStatusCommand>
{
    public ChangeCaseStatusCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.TargetStatus).IsInEnum().WithMessage("Gecersiz hedef durum.");
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
