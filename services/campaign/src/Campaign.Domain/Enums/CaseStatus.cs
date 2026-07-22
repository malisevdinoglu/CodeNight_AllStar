namespace Campaign.Domain.Enums;

/// <summary>State machine gecis kurallari Core_Principles §7'de — kural disi gecis 422.</summary>
public enum CaseStatus
{
    YENI,
    ATANDI,
    OPTIMIZE_EDILIYOR,
    TEST_EDILIYOR,
    TAMAMLANDI,
    YAYINDA,
    ARSIVLENDI
}
