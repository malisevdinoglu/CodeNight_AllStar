using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Commands.VerifyOtp;

/// <summary>Case §3.1: OTP simülasyonu — sabit kod "1234". Başarılıysa kullanıcı aktifleşir + token çifti döner.</summary>
public sealed record VerifyOtpCommand(string GsmNumber, string OtpCode) : IRequest<AuthResultDto>;
