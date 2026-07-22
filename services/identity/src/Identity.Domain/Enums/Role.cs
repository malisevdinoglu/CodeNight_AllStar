namespace Identity.Domain.Enums;

/// <summary>
/// Case dokümanı §1.2 / Core_Principles §6 yetki matrisi ile birebir.
/// EF Core bunu <c>HasConversion&lt;string&gt;()</c> ile aynen bu isimlerle (Türkçe UPPER_SNAKE) saklar —
/// çeviri YASAK (Core_Principles §4).
/// </summary>
public enum Role
{
    MUSTERI,
    PERSONEL,
    SUPERVIZOR,
    ADMIN
}
