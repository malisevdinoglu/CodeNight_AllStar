using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Commands.Login;

/// <summary>Case §3.1: e-posta + şifre (personel/süpervizör/admin). 5 hatalı denemede 15 dk kilit.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
