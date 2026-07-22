using Campaign.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Campaign.UnitTests.Domain;

/// <summary>Iskender.md §2: CMP-2026-000123 / OPT-2026-000045.</summary>
public class NumberFormatterTests
{
    [Fact]
    public void FormatCampaignNumber_altı_haneli_sifir_dolgulu_uretmeli()
    {
        NumberFormatter.FormatCampaignNumber(2026, 123).Should().Be("CMP-2026-000123");
    }

    [Fact]
    public void FormatCaseNumber_altı_haneli_sifir_dolgulu_uretmeli()
    {
        NumberFormatter.FormatCaseNumber(2026, 45).Should().Be("OPT-2026-000045");
    }

    [Fact]
    public void FormatCampaignNumber_6_haneyi_asan_degerde_dolgu_yapmadan_uzatmali()
    {
        NumberFormatter.FormatCampaignNumber(2026, 1234567).Should().Be("CMP-2026-1234567");
    }
}
