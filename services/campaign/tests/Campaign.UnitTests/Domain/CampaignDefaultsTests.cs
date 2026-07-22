using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Campaign.UnitTests.Domain;

public class CampaignDefaultsTests
{
    [Theory]
    [InlineData(CampaignType.EK_PAKET, 20)]
    [InlineData(CampaignType.TARIFE_YUKSELTME, 15)]
    [InlineData(CampaignType.CIHAZ_FIRSATI, 25)]
    [InlineData(CampaignType.SADAKAT, 30)]
    public void GetDefaultDiscountRate_her_tur_icin_dokumante_edilmis_deger_dondurmeli(
        CampaignType type, decimal expected)
    {
        CampaignDefaults.GetDefaultDiscountRate(type).Should().Be(expected);
    }

    [Fact]
    public void GetDefaultValidUntil_30_gun_sonrasini_dondurmeli()
    {
        var validFrom = new DateOnly(2026, 7, 22);

        CampaignDefaults.GetDefaultValidUntil(validFrom).Should().Be(new DateOnly(2026, 8, 21));
    }

    [Fact]
    public void DeterminePriorityFromConversion_null_icin_ORTA_dondurmeli_fallback()
    {
        CampaignDefaults.DeterminePriorityFromConversion(null).Should().Be(CasePriority.ORTA);
    }

    [Theory]
    [InlineData(0.10, CasePriority.KRITIK)]
    [InlineData(0.29, CasePriority.KRITIK)]
    [InlineData(0.30, CasePriority.YUKSEK)]
    [InlineData(0.49, CasePriority.YUKSEK)]
    [InlineData(0.50, CasePriority.ORTA)]
    [InlineData(0.69, CasePriority.ORTA)]
    [InlineData(0.70, CasePriority.DUSUK)]
    [InlineData(0.95, CasePriority.DUSUK)]
    public void DeterminePriorityFromConversion_esik_sinirlarinda_dogru_kategorize_etmeli(
        double avgConversionProbability, CasePriority expected)
    {
        CampaignDefaults.DeterminePriorityFromConversion((decimal)avgConversionProbability).Should().Be(expected);
    }
}
