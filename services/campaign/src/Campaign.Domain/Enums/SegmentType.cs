namespace Campaign.Domain.Enums;

/// <summary>BELIRSIZ: AI Service kapaliyken atanan fallback segment (graceful degradation).</summary>
public enum SegmentType
{
    YUKSEK_DEGER,
    RISKLI_KAYIP,
    YENI_ABONE,
    PASIF,
    BELIRSIZ
}
