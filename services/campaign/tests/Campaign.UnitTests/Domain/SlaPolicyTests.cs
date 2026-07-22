using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Campaign.UnitTests.Domain;

/// <summary>Core_Principles §7: KRITIK 2 saat, YUKSEK 8 saat, ORTA 24 saat, DUSUK 72 saat.</summary>
public class SlaPolicyTests
{
    [Theory]
    [InlineData(CasePriority.KRITIK, 2)]
    [InlineData(CasePriority.YUKSEK, 8)]
    [InlineData(CasePriority.ORTA, 24)]
    [InlineData(CasePriority.DUSUK, 72)]
    public void GetDuration_dogru_saat_dondurmeli(CasePriority priority, int expectedHours)
    {
        SlaPolicy.GetDuration(priority).Should().Be(TimeSpan.FromHours(expectedHours));
    }

    [Fact]
    public void CalculateDeadline_createdAt_uzerine_suresi_ekler()
    {
        var createdAt = new DateTimeOffset(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

        var deadline = SlaPolicy.CalculateDeadline(CasePriority.KRITIK, createdAt);

        deadline.Should().Be(createdAt.AddHours(2));
    }

    [Theory]
    [InlineData(CasePriority.DUSUK)]
    [InlineData(CasePriority.ORTA)]
    public void RISKLI_KAYIP_segmentinde_DUSUK_veya_ORTA_YUKSEK_e_yukseltilmeli(CasePriority computed)
    {
        var result = SlaPolicy.ApplyMinimumForSegment(computed, SegmentType.RISKLI_KAYIP);

        result.Should().Be(CasePriority.YUKSEK);
    }

    [Theory]
    [InlineData(CasePriority.YUKSEK)]
    [InlineData(CasePriority.KRITIK)]
    public void RISKLI_KAYIP_segmentinde_YUKSEK_veya_uzeri_DEGISMEMELI(CasePriority computed)
    {
        var result = SlaPolicy.ApplyMinimumForSegment(computed, SegmentType.RISKLI_KAYIP);

        result.Should().Be(computed);
    }

    [Fact]
    public void RISKLI_KAYIP_disindaki_segmentlerde_oncelik_degismemeli()
    {
        var result = SlaPolicy.ApplyMinimumForSegment(CasePriority.DUSUK, SegmentType.PASIF);

        result.Should().Be(CasePriority.DUSUK);
    }

    [Fact]
    public void RemainingSeconds_deadline_gecmisse_sifir_dondurmeli_negatif_degil()
    {
        var now = DateTimeOffset.UtcNow;
        var pastDeadline = now.AddHours(-1);

        SlaPolicy.RemainingSeconds(pastDeadline, now).Should().Be(0);
    }

    [Fact]
    public void RemainingSeconds_deadline_gelecekteyse_pozitif_dondurmeli()
    {
        var now = DateTimeOffset.UtcNow;
        var futureDeadline = now.AddMinutes(30);

        SlaPolicy.RemainingSeconds(futureDeadline, now).Should().Be(1800);
    }
}
