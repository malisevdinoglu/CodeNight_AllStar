using FluentValidation;

namespace Identity.Application.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("E-posta formati gecersiz.");

        RuleFor(x => x.Password).NotEmpty().WithMessage("Sifre zorunludur.");
    }
}
