namespace Identity.Domain.Enums;

/// <summary>
/// DB'de ve JWT'de string olarak saklanir (Core_Principles §4: Türkçe UPPER_SNAKE aynen).
/// </summary>
public enum UserRole
{
    MUSTERI,
    PERSONEL,
    SUPERVIZOR,
    ADMIN
}
