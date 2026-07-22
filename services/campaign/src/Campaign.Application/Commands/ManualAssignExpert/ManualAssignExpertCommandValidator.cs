using FluentValidation;

namespace Campaign.Application.Commands.ManualAssignExpert;

public sealed class ManualAssignExpertCommandValidator : AbstractValidator<ManualAssignExpertCommand>
{
    public ManualAssignExpertCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ExpertId).NotEmpty();
    }
}
