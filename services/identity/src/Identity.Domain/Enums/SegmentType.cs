namespace Identity.Domain.Enums;

/// <summary>
/// Personelin uzmanlık alanı (Iskender.md §1: user_expertises.segment_type).
/// Campaign/AI servislerindeki abone segmentiyle AYNI kelime kümesi — çeviri YASAK.
/// </summary>
public enum SegmentType
{
    YUKSEK_DEGER,
    RISKLI_KAYIP,
    YENI_ABONE,
    PASIF
}
