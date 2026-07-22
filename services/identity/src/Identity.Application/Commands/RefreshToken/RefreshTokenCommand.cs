using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Commands.RefreshToken;

/// <summary>
/// Core_Principles §10: rotation zorunlu. Revoke edilmiş bir token tekrar kullanılırsa
/// kullanıcının TÜM oturumları kapatılır (theft koruması) — jüri bunu canlı deneyecek.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
