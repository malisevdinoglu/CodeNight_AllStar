using FluentAssertions;
using Gamification.Domain.Services;
using Xunit;

namespace Gamification.UnitTests.Domain;

/// <summary>Core_Principles §8 puan tablosu: TAMAMLANDI +10, süre&lt;2s +5, lift&gt;hedef +15, KRITIK&amp;SLA-içi +15.</summary>
public sealed class PointRuleEngineTests
{
    private static readonly DateTimeOffset Created = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Sadece_tamamlandi_bonussuz_10_puan_verir()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "ORTA", conversionLift: 2m, Created, Created.AddHours(3), hadPriorSlaBreach: false);

        result.TotalPoints.Should().Be(10);
        result.FastCompletion.Should().BeFalse();
        result.TargetExceeded.Should().BeFalse();
        result.KritikSlaBonusApplied.Should().BeFalse();
    }

    [Fact]
    public void Iki_saatin_altinda_tamamlanirsa_5_bonus_puan_ekler()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "ORTA", conversionLift: null, Created, Created.AddHours(1).AddMinutes(59), hadPriorSlaBreach: false);

        result.TotalPoints.Should().Be(15);
        result.FastCompletion.Should().BeTrue();
    }

    [Fact]
    public void Tam_iki_saatte_tamamlanirsa_hiz_bonusu_TETIKLENMEZ()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "ORTA", conversionLift: null, Created, Created.AddHours(2), hadPriorSlaBreach: false);

        result.TotalPoints.Should().Be(10);
        result.FastCompletion.Should().BeFalse();
    }

    [Fact]
    public void Hedef_donusumu_asarsa_15_bonus_puan_ekler()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "ORTA", conversionLift: 5.01m, Created, Created.AddHours(3), hadPriorSlaBreach: false);

        result.TotalPoints.Should().Be(25);
        result.TargetExceeded.Should().BeTrue();
    }

    [Fact]
    public void Hedefe_tam_esitse_asilmis_SAYILMAZ()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "ORTA", conversionLift: 5.0m, Created, Created.AddHours(3), hadPriorSlaBreach: false);

        result.TargetExceeded.Should().BeFalse();
        result.TotalPoints.Should().Be(10);
    }

    [Fact]
    public void Kritik_oncelik_ve_hic_SLA_ihlali_yoksa_15_bonus_puan_ekler()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "KRITIK", conversionLift: null, Created, Created.AddHours(3), hadPriorSlaBreach: false);

        result.TotalPoints.Should().Be(25);
        result.KritikSlaBonusApplied.Should().BeTrue();
    }

    [Fact]
    public void Kritik_oncelik_ama_daha_once_SLA_ihlali_varsa_bonus_VERILMEZ()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "KRITIK", conversionLift: null, Created, Created.AddHours(3), hadPriorSlaBreach: true);

        result.TotalPoints.Should().Be(10);
        result.KritikSlaBonusApplied.Should().BeFalse();
    }

    [Fact]
    public void Tum_bonuslar_bir_arada_tetiklenebilir_ve_toplanir()
    {
        var result = PointRuleEngine.CalculateCampaignOptimizedPoints(
            "KRITIK", conversionLift: 9.5m, Created, Created.AddMinutes(30), hadPriorSlaBreach: false);

        // 10 (taban) + 5 (hiz) + 15 (hedef asimi) + 15 (kritik&SLA-ici) = 45
        result.TotalPoints.Should().Be(45);
        result.FastCompletion.Should().BeTrue();
        result.TargetExceeded.Should().BeTrue();
        result.KritikSlaBonusApplied.Should().BeTrue();
    }
}
