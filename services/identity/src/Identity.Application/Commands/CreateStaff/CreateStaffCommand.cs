using Identity.Application.Dtos;
using MediatR;

namespace Identity.Application.Commands.CreateStaff;

/// <summary>Case §3.1: sadece ADMIN. Uzmanlık/bölge alanları zorunlu (Mali.md §4).</summary>
public sealed record CreateStaffCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role,
    string Region,
    IReadOnlyList<string> Expertise) : IRequest<UserSummaryDto>;
