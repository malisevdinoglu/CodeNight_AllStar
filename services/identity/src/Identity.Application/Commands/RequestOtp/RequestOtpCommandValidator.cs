using FluentValidation;

namespace Identity.Application.Commands.RequestOtp;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.GsmNumber)
            .NotEmpty().WithMessage("GSM numarasi zorunludur.")
            .Matches(@"^5\d{9}$").WithMessage("GSM numarasi 5 ile baslayan 10 haneli olmalidir.");
    }
}
