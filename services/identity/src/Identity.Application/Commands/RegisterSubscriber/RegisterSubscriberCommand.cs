using MediatR;

namespace Identity.Application.Commands.RegisterSubscriber;

/// <summary>Case §3.1: ad, soyad, GSM, e-posta (opsiyonel). OTP akışı başlatılır (sabit kod 1234, simülasyon).</summary>
public sealed record RegisterSubscriberCommand(
    string FirstName,
    string LastName,
    string GsmNumber,
    string? Email) : IRequest<RegisterSubscriberResult>;

public sealed record RegisterSubscriberResult(Guid UserId, string GsmNumber);
