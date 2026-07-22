using Campaign.Domain.Entities;
using Campaign.Domain.Enums;

namespace Campaign.Domain.Services;

/// <summary>
/// Core_Principles §7'nin TEK doğru kaynağı: vaka state machine'i. Bu tablo dışındaki
/// her geçiş 422 CMP_422_INVALID_TRANSITION'dır; rol/sahiplik uymuyorsa 403 + audit.
/// Domain kuralı olduğu için burada yaşar (Core_Principles §4) — entity'ler anemik
/// (İskender.md/İskender'in EF şeması) olsa da geçiş KURALI ve MUTASYONU domain'de tek yerde toplanır.
/// </summary>
public static class CaseStateMachine
{
    private static readonly IReadOnlyList<CaseTransitionRule> Rules = new[]
    {
        // Sistem (AI atama basarili) veya SUPERVIZOR (manuel atama) - kosul: uzman belirlendi.
        new CaseTransitionRule(CaseStatus.YENI, CaseStatus.ATANDI,
            new[] { CaseTransitionActor.System, CaseTransitionActor.Supervizor }),

        new CaseTransitionRule(CaseStatus.ATANDI, CaseStatus.OPTIMIZE_EDILIYOR,
            new[] { CaseTransitionActor.AssignedExpert }),

        new CaseTransitionRule(CaseStatus.OPTIMIZE_EDILIYOR, CaseStatus.TEST_EDILIYOR,
            new[] { CaseTransitionActor.AssignedExpert }),

        // Spesifikasyon "Sistem" der; otomatik A/B test sonuc servisi bu fazin kapsaminda
        // tanimlanmadigindan, demo/jüri akisinin calismasi icin atanan uzmanin da
        // test sonucunu manuel bildirmesine izin verilir (pragmatik, dokumante edilmis secim).
        new CaseTransitionRule(CaseStatus.TEST_EDILIYOR, CaseStatus.OPTIMIZE_EDILIYOR,
            new[] { CaseTransitionActor.System, CaseTransitionActor.AssignedExpert }),

        new CaseTransitionRule(CaseStatus.OPTIMIZE_EDILIYOR, CaseStatus.TAMAMLANDI,
            new[] { CaseTransitionActor.AssignedExpert }, RequiresExpertNote: true),

        new CaseTransitionRule(CaseStatus.TAMAMLANDI, CaseStatus.YAYINDA,
            new[] { CaseTransitionActor.Supervizor }),

        new CaseTransitionRule(CaseStatus.YAYINDA, CaseStatus.ARSIVLENDI,
            new[] { CaseTransitionActor.System }),
    };

    public static CaseTransitionRule? FindRule(CaseStatus from, CaseStatus to) =>
        Rules.FirstOrDefault(r => r.From == from && r.To == to);

    /// <summary>CaseDto.allowedTransitions için (frontend'in "hangi butonlar aktif" sorusu).</summary>
    public static IReadOnlyList<CaseStatus> GetAllowedNextStatuses(CaseStatus from) =>
        Rules.Where(r => r.From == from).Select(r => r.To).ToList();

    /// <summary>
    /// Tek mutasyon noktasi: rol/sahiplik dogrulamasi caller'da yapilmis olmali (Application katmani,
    /// çünkü ICurrentRequestContext'e/repository'ye erişim Domain'in bilmediği şeylerdir).
    /// TAMAMLANDI'da SLA sayacı durur (CompletedAt set edilir).
    /// </summary>
    public static CaseStatusHistory Apply(
        OptimizationCase @case, CaseStatus to, Guid changedBy, string? note, DateTimeOffset nowUtc)
    {
        var history = new CaseStatusHistory
        {
            CaseId = @case.Id,
            FromStatus = @case.Status,
            ToStatus = to,
            ChangedBy = changedBy,
            Note = note,
            ChangedAt = nowUtc,
        };

        @case.Status = to;
        if (to == CaseStatus.TAMAMLANDI)
        {
            @case.CompletedAt = nowUtc;
        }

        return history;
    }
}
