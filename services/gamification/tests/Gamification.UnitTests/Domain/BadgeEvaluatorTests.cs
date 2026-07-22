using FluentAssertions;
using Gamification.Domain.Constants;
using Gamification.Domain.Services;
using Xunit;

namespace Gamification.UnitTests.Domain;

public sealed class BadgeEvaluatorTests
{
    private static readonly IReadOnlySet<string> NoBadgesYet = new HashSet<string>();

    private static BadgeEvaluationContext MakeContext(
        int completedCaseCount = 0,
        int fastCompletionCount = 0,
        int targetExceededCount = 0,
        int riskliKayipSavedCount = 0,
        int todayCompletedCount = 0,
        int currentSegmentCompletedCount = 0,
        IReadOnlySet<string>? alreadyEarned = null) =>
        new(completedCaseCount, fastCompletionCount, targetExceededCount, riskliKayipSavedCount,
            todayCompletedCount, currentSegmentCompletedCount, alreadyEarned ?? NoBadgesYet);

    [Fact]
    public void Hicbir_esik_karsilanmazsa_bos_liste_doner()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(MakeContext());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Ilk_tamamlanan_vaka_ILK_KAMPANYA_kazandirir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(MakeContext(completedCaseCount: 1));

        result.Should().ContainSingle().Which.Should().Be(BadgeCodes.IlkKampanya);
    }

    [Fact]
    public void Zaten_kazanilmis_rozet_TEKRAR_donmez()
    {
        var alreadyEarned = new HashSet<string> { BadgeCodes.IlkKampanya };

        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 5, alreadyEarned: alreadyEarned));

        result.Should().NotContain(BadgeCodes.IlkKampanya);
    }

    [Fact]
    public void Hiz_ustasi_esigi_10da_tetiklenir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 10, fastCompletionCount: 10));

        result.Should().Contain(BadgeCodes.HizUstasi);
    }

    [Fact]
    public void Donusum_krali_esigi_10da_tetiklenir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 10, targetExceededCount: 10));

        result.Should().Contain(BadgeCodes.DonusumKrali);
    }

    [Fact]
    public void Maratoncu_esigi_gunde_20de_tetiklenir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 20, todayCompletedCount: 20));

        result.Should().Contain(BadgeCodes.Maratoncu);
    }

    [Fact]
    public void Churn_avcisi_esigi_10da_tetiklenir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 10, riskliKayipSavedCount: 10));

        result.Should().Contain(BadgeCodes.ChurnAvcisi);
    }

    [Fact]
    public void Uzman_esigi_tek_segmentte_50de_tetiklenir()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(
            MakeContext(completedCaseCount: 50, currentSegmentCompletedCount: 50));

        result.Should().Contain(BadgeCodes.Uzman);
    }

    [Fact]
    public void Birden_fazla_esik_ayni_anda_karsilanirsa_hepsi_doner()
    {
        var result = BadgeEvaluator.EvaluateNewlyEarned(new BadgeEvaluationContext(
            CompletedCaseCount: 20,
            FastCompletionCount: 10,
            TargetExceededCount: 10,
            RiskliKayipSavedCount: 10,
            TodayCompletedCount: 20,
            CurrentSegmentCompletedCount: 50,
            AlreadyEarnedBadgeCodes: NoBadgesYet));

        result.Should().BeEquivalentTo(new[]
        {
            BadgeCodes.IlkKampanya,
            BadgeCodes.HizUstasi,
            BadgeCodes.DonusumKrali,
            BadgeCodes.Maratoncu,
            BadgeCodes.ChurnAvcisi,
            BadgeCodes.Uzman,
        });
    }
}
