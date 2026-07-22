using FluentValidation;

namespace Identity.Application.Commands.VerifyOtp;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.GsmNumber)
            .NotEmpty().WithMessage("GSM numarasi zorunludur.")
            .Matches(@"^5\d{9}$").WithMessage("GSM numarasi 5 ile baslayan 10 haneli olmalidir.");

        RuleFor(x => x.OtpCode).NotEmpty().WithMessage("OTP kodu zorunludur.");
    }
}
