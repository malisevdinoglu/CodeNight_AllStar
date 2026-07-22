namespace BuildingBlocks.Exceptions;

/// <summary>
/// Varsayılan 422 domain kural ihlali (Core_Principles §5: "kural dışı state geçişi").
/// Örnek: <c>throw new DomainRuleException("INVALID_TRANSITION", "TAMAMLANDI durumundan ATANDI durumuna geçilemez.")</c>
/// → hata kodu servis prefix'iyle birleşince <c>CMP_422_INVALID_TRANSITION</c> olur.
/// </summary>
public sealed class DomainRuleException : DomainException
{
    public DomainRuleException(string errorCode, string message, IReadOnlyList<string>? details = null)
        : base(errorCode, message, details)
    {
    }
}
