namespace Identity.Application.Common;

/// <summary>BCrypt work factor 11 (Core_Principles §10) — Infrastructure implemente eder.</summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);

    bool Verify(string plainPassword, string hash);
}
