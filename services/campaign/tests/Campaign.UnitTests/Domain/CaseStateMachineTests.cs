using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Campaign.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Campaign.UnitTests.Domain;

/// <summary>
/// Core_Principles §7 tablosunun BİREBİR uygulanması: her geçerli geçiş beklenen aktörlerle
/// izinli, tablo dışındaki HER (from,to) çifti kuraldışı (422 CMP_422_INVALID_TRANSITION'a
/// dönüşecek şekilde FindRule null döner). Mali.md'nin özellikle istediği "invalid-transition
/// matrisi" burada tam kapsanır.
/// </summary>
public class CaseStateMachineTests
{
    public static IEnumerable<object[]> ValidTransitions()
    {
        yield return new object[] { CaseStatus.YENI, CaseStatus.ATANDI };
        yield return new object[] { CaseStatus.ATANDI, CaseStatus.OPTIMIZE_EDILIYOR };
        yield return new object[] { CaseStatus.OPTIMIZE_EDILIYOR, CaseStatus.TEST_EDILIYOR };
        yield return new object[] { CaseStatus.TEST_EDILIYOR, CaseStatus.OPTIMIZE_EDILIYOR };
        yield return new object[] { CaseStatus.OPTIMIZE_EDILIYOR, CaseStatus.TAMAMLANDI };
        yield return new object[] { CaseStatus.TAMAMLANDI, CaseStatus.YAYINDA };
        yield return new object[] { CaseStatus.YAYINDA, CaseStatus.ARSIVLENDI };
    }

    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void Tabloda_tanimli_gecisler_izinli_olmali(CaseStatus from, CaseStatus to)
    {
        var rule = CaseStateMachine.FindRule(from, to);

        rule.Should().NotBeNull();
        rule!.From.Should().Be(from);
        rule.To.Should().Be(to);
    }

    public static IEnumerable<object[]> AllStatusPairs()
    {
        var statuses = Enum.GetValues<CaseStatus>();
        foreach (var from in statuses)
        {
            foreach (var to in statuses)
            {
                yield return new object[] { from, to };
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllStatusPairs))]
    public void Tablo_disindaki_HER_gecis_null_rule_dondurmeli(CaseStatus from, CaseStatus to)
    {
        var isValidPair = ValidTransitions().Any(t => (CaseStatus)t[0] == from && (CaseStatus)t[1] == to);
        var rule = CaseStateMachine.FindRule(from, to);

        if (isValidPair)
        {
            rule.Should().NotBeNull();
        }
        else
        {
            rule.Should().BeNull($"{from} -> {to} tabloda tanimli degil, 422 CMP_422_INVALID_TRANSITION olmali");
        }
    }

    [Fact]
    public void OPTIMIZE_EDILIYOR_TAMAMLANDI_gecisi_expertNote_zorunlu_isaretlenmeli()
    {
        var rule = CaseStateMachine.FindRule(CaseStatus.OPTIMIZE_EDILIYOR, CaseStatus.TAMAMLANDI);

        rule.Should().NotBeNull();
        rule!.RequiresExpertNote.Should().BeTrue();
    }

    [Fact]
    public void Diger_gecisler_expertNote_zorunlu_OLMAMALI()
    {
        var rule = CaseStateMachine.FindRule(CaseStatus.YENI, CaseStatus.ATANDI);

        rule.Should().NotBeNull();
        rule!.RequiresExpertNote.Should().BeFalse();
    }

    [Theory]
    [InlineData(CaseStatus.YENI, CaseTransitionActor.System)]
    [InlineData(CaseStatus.YENI, CaseTransitionActor.Supervizor)]
    public void YENI_ATANDI_gecisi_sistem_veya_supervizor_actor_kabul_etmeli(CaseStatus from, CaseTransitionActor actor)
    {
        var rule = CaseStateMachine.FindRule(from, CaseStatus.ATANDI);

        rule.Should().NotBeNull();
        rule!.AllowedActors.Should().Contain(actor);
    }

    [Fact]
    public void YENI_ATANDI_gecisi_AssignedExpert_actor_KABUL_ETMEMELI()
    {
        var rule = CaseStateMachine.FindRule(CaseStatus.YENI, CaseStatus.ATANDI);

        rule.Should().NotBeNull();
        rule!.AllowedActors.Should().NotContain(CaseTransitionActor.AssignedExpert);
    }

    [Fact]
    public void Apply_durumu_gunceller_ve_history_dondurur()
    {
        var optimizationCase = new OptimizationCase
        {
            Id = Guid.NewGuid(),
            Status = CaseStatus.YENI,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var changedBy = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var history = CaseStateMachine.Apply(optimizationCase, CaseStatus.ATANDI, changedBy, "not", now);

        optimizationCase.Status.Should().Be(CaseStatus.ATANDI);
        history.FromStatus.Should().Be(CaseStatus.YENI);
        history.ToStatus.Should().Be(CaseStatus.ATANDI);
        history.ChangedBy.Should().Be(changedBy);
        history.Note.Should().Be("not");
        history.ChangedAt.Should().Be(now);
    }

    [Fact]
    public void Apply_TAMAMLANDI_gecisinde_CompletedAt_set_edilmeli()
    {
        var optimizationCase = new OptimizationCase
        {
            Id = Guid.NewGuid(),
            Status = CaseStatus.OPTIMIZE_EDILIYOR,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        };
        var now = DateTimeOffset.UtcNow;

        CaseStateMachine.Apply(optimizationCase, CaseStatus.TAMAMLANDI, Guid.NewGuid(), "tamam", now);

        optimizationCase.CompletedAt.Should().Be(now);
    }

    [Fact]
    public void Apply_TAMAMLANDI_disindaki_gecislerde_CompletedAt_set_edilmemeli()
    {
        var optimizationCase = new OptimizationCase
        {
            Id = Guid.NewGuid(),
            Status = CaseStatus.YENI,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        CaseStateMachine.Apply(optimizationCase, CaseStatus.ATANDI, Guid.NewGuid(), null, DateTimeOffset.UtcNow);

        optimizationCase.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void GetAllowedNextStatuses_YENI_icin_sadece_ATANDI_dondurmeli()
    {
        var allowed = CaseStateMachine.GetAllowedNextStatuses(CaseStatus.YENI);

        allowed.Should().ContainSingle().Which.Should().Be(CaseStatus.ATANDI);
    }

    [Fact]
    public void GetAllowedNextStatuses_ARSIVLENDI_icin_bos_olmali_terminal_state()
    {
        var allowed = CaseStateMachine.GetAllowedNextStatuses(CaseStatus.ARSIVLENDI);

        allowed.Should().BeEmpty();
    }
}
