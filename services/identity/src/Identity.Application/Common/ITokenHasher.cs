namespace Identity.Application.Common;

/// <summary>
/// Refresh token'lar bcrypt DEĞİL, SHA-256 ile hash'lenir (Iskender.md §1 — hızlı karşılaştırma,
/// zaten yüksek entropili rastgele token için maliyetli hash gereksiz).
/// </summary>
public interface ITokenHasher
{
    string Sha256(string plainToken);
}
