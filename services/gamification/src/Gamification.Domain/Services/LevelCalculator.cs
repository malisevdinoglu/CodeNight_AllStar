using Gamification.Domain.Constants;
using Gamification.Domain.Enums;

namespace Gamification.Domain.Services;

/// <summary>Mali.md §7: seviye toplam puandan hesaplanır - saklanmaz, her okumada türetilir.</summary>
public static class LevelCalculator
{
    public static ExpertLevel Calculate(int totalPoints) => totalPoints switch
    {
        _ when totalPoints > GamificationDefaults.AltinMax => ExpertLevel.PLATIN,
        _ when totalPoints > GamificationDefaults.GumusMax => ExpertLevel.ALTIN,
        _ when totalPoints > GamificationDefaults.BronzMax => ExpertLevel.GUMUS,
        _ => ExpertLevel.BRONZ,
    };
}
