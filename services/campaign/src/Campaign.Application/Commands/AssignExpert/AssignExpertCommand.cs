using MediatR;

namespace Campaign.Application.Commands.AssignExpert;

/// <summary>
/// Mali_Plan.md: "Düşük dönüşüm tahminli segment için AssignExpertCommand tetikle → AI /ai/assign
/// → en yüksek skorlu uzmana ata → case.assigned; kapasite yoksa kuyruk (status YENI kalır)."
/// Sistem içi komut — HTTP endpoint'i yoktur (CreateCampaignCommandHandler tetikler); ileride
/// SUPERVIZOR'ün manuel yeniden-atama tetiklemesi için de yeniden kullanılabilir.
/// Sonuç: true = uzmana atandı (case.assigned yayınlandı), false = kapasite/AI yok, case YENI kaldı.
/// </summary>
public sealed record AssignExpertCommand(Guid CaseId) : IRequest<bool>;
