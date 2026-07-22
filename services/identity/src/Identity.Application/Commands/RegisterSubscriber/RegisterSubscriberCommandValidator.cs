using FluentValidation;

namespace Identity.Application.Commands.RegisterSubscriber;

public sealed class RegisterSubscriberCommandValidator : AbstractValidator<RegisterSubscriberCommand>
{
    public RegisterSubscriberCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60).WithMessage("Ad zorunludur (en fazla 60 karakter).");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60).WithMessage("Soyad zorunludur (en fazla 60 karakter).");

        RuleFor(x => x.GsmNumber)
            .NotEmpty().WithMessage("GSM numarasi zorunludur.")
            .Matches(@"^5\d{9}$").WithMessage("GSM numarasi 5 ile baslayan 10 haneli olmalidir (orn. 5551234567).");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-posta formati gecersiz.")
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
