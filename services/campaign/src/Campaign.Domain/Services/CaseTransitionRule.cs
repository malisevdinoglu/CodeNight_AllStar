using Campaign.Domain.Enums;

namespace Campaign.Domain.Services;

/// <summary>Bir state geçişini kimin tetikleyebileceği (Core_Principles §7).</summary>
public enum CaseTransitionActor
{
    /// <summary>Vakaya atanmış PERSONEL (sahiplik kontrolü: assignedExpertId == token.sub).</summary>
    AssignedExpert,
    Supervizor,

    /// <summary>AI/zamanlayıcı gibi sistem içi tetikleyiciler — HTTP rol kontrolüne tabi değildir.</summary>
    System
}

public sealed record CaseTransitionRule(
    CaseStatus From,
    CaseStatus To,
    IReadOnlyList<CaseTransitionActor> AllowedActors,
    bool RequiresExpertNote = false);
