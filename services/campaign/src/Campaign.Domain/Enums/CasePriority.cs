namespace Campaign.Domain.Enums;

/// <summary>SLA sureleri: KRITIK 2s, YUKSEK 8s, ORTA 24s, DUSUK 72s (case §4.4).</summary>
public enum CasePriority
{
    DUSUK,
    ORTA,
    YUKSEK,
    KRITIK
}
