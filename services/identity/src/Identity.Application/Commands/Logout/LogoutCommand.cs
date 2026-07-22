using MediatR;

namespace Identity.Application.Commands.Logout;

/// <summary>Case §3.2: refresh token geçersiz kılınır. İdempotent — token zaten yoksa da başarı döner.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest;
