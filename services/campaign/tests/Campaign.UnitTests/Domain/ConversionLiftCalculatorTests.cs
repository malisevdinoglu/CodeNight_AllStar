using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Campaign.UnitTests.Domain;

public class ConversionLiftCalculatorTests
{
    private static Offer MakeOffer(OfferStatus status, decimal predictedProbability) => new()
    {
        Id = Guid.NewGuid(),
        CampaignId = Guid.NewGuid(),
        SubscriberId = Guid.NewGuid(),
        RecommendationScore = 0.70m,
        ConversionProbability = predictedProbability,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Teklif_yoksa_null_dondurmeli()
    {
        ConversionLiftCalculator.Calculate(Array.Empty<Offer>()).Should().BeNull();
    }

    [Fact]
    public void Gercek_kabul_orani_tahminden_yuksekse_pozitif_lift_dondurmeli()
    {
        // 2 teklif, ikisi de KABUL (gercek oran = 1.00), tahmin ortalamasi 0.60 -> lift = +40.00
        var offers = new[]
        {
            MakeOffer(OfferStatus.KABUL, 0.60m),
            MakeOffer(OfferStatus.KABUL, 0.60m),
        };

        var lift = ConversionLiftCalculator.Calculate(offers);

        lift.Should().Be(40.00m);
    }

    [Fact]
    public void Gercek_kabul_orani_tahminden_dusukse_negatif_lift_dondurmeli()
    {
        // 2 teklif, hicbiri KABUL (gercek oran = 0.00), tahmin ortalamasi 0.60 -> lift = -60.00
        var offers = new[]
        {
            MakeOffer(OfferStatus.RET, 0.60m),
            MakeOffer(OfferStatus.SUNULDU, 0.60m),
        };

        var lift = ConversionLiftCalculator.Calculate(offers);

        lift.Should().Be(-60.00m);
    }

    [Fact]
    public void Karisik_sonuclarda_doğru_hesaplanmali()
    {
        // 4 teklif, 1 KABUL (gercek oran = 0.25), tahmin ortalamasi 0.50 -> lift = -25.00
        var offers = new[]
        {
            MakeOffer(OfferStatus.KABUL, 0.50m),
            MakeOffer(OfferStatus.RET, 0.50m),
            MakeOffer(OfferStatus.RET, 0.50m),
            MakeOffer(OfferStatus.SUNULDU, 0.50m),
        };

        var lift = ConversionLiftCalculator.Calculate(offers);

        lift.Should().Be(-25.00m);
    }
}
