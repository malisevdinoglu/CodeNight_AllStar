using FluentAssertions;
using Gamification.Domain.Enums;
using Gamification.Domain.Services;
using Xunit;

namespace Gamification.UnitTests.Domain;

/// <summary>Mali.md §7: Bronz 0-499, Gümüş 500-1499, Altın 1500-2999, Platin 3000+.</summary>
public sealed class LevelCalculatorTests
{
    [Theory]
    [InlineData(0, ExpertLevel.BRONZ)]
    [InlineData(499, ExpertLevel.BRONZ)]
    [InlineData(500, ExpertLevel.GUMUS)]
    [InlineData(1499, ExpertLevel.GUMUS)]
    [InlineData(1500, ExpertLevel.ALTIN)]
    [InlineData(2999, ExpertLevel.ALTIN)]
    [InlineData(3000, ExpertLevel.PLATIN)]
    [InlineData(50000, ExpertLevel.PLATIN)]
    public void Esik_sinirlarinda_dogru_seviyeyi_dondurur(int totalPoints, ExpertLevel expected)
    {
        LevelCalculator.Calculate(totalPoints).Should().Be(expected);
    }
}
