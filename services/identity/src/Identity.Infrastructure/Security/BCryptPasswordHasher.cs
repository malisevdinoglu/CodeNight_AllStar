using Identity.Application.Common;

namespace Identity.Infrastructure.Security;

/// <summary>Core_Principles §10 / Mali.md §4: BCrypt.Net-Next, work factor 11 (sabit).</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: WorkFactor);

    public bool Verify(string plainPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
