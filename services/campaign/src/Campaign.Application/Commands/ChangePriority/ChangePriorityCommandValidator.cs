using FluentValidation;

namespace Campaign.Application.Commands.ChangePriority;

public sealed class ChangePriorityCommandValidator : AbstractValidator<ChangePriorityCommand>
{
    public ChangePriorityCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum().WithMessage("Gecersiz oncelik.");
    }
}
