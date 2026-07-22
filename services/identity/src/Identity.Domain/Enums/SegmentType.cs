namespace Identity.Domain.Enums;

/// <summary>
/// Personelin uzmanlik alani. BELIRSIZ burada yoktur — o Campaign tarafinin
/// AI-kapali fallback degeridir, bir uzmanlik alani degildir.
/// </summary>
public enum SegmentType
{
    YUKSEK_DEGER,
    RISKLI_KAYIP,
    YENI_ABONE,
    PASIF
}
