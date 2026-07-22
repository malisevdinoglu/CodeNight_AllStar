using System.Security.Cryptography;
using System.Text;
using Identity.Application.Common;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Refresh token'lar için SHA-256 (Iskender.md §1: token_hash varchar(64) — hex çıktı 64 karakter).
/// </summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Sha256(string plainToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
