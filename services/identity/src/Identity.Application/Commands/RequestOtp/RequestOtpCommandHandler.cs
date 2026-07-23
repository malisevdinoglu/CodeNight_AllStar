using MediatR;

namespace Identity.Application.Commands.RequestOtp;

/// <summary>
/// GSM format dogrulamasi ValidationBehavior pipeline'inda (RequestOtpCommandValidator) zaten
/// yapildi. OTP simule edildigi (sabit "1234") ve numara varligi sizdirilmadigi icin burada
/// baska bir is yok - sadece basariyla tamamlanir, boylece frontend kod giris adimina gecer.
/// </summary>
public sealed class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand>
{
    public Task Handle(RequestOtpCommand request, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
