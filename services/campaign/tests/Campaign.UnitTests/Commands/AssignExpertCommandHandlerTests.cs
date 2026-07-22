using Campaign.Application.Commands.AssignExpert;
using Campaign.Application.Common;
using Campaign.Application.Events;
using Campaign.Application.External;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using Xunit;

namespace Campaign.UnitTests.Commands;

/// <summary>
/// Mali.md §9: "atama skor formülü" unit test önceliği. AssignExpertCommandHandler'ın
/// kapasite filtresi (MaxActiveCasesPerExpert=5), AI'ya gönderilen PerformanceScore formülü
/// (1 - min(1, aktifVaka/5)) ve AI kapalıyken fallback sıralaması (en boş kapasiteli uzman)
/// daha önce hiç test edilmemişti — bu dosya o boşluğu kapatır.
/// NOT: Bu proje bu sandbox'ta derlenemedi (.NET SDK yok) — `dotnet test` ile doğrulanmalı.
/// </summary>
public sealed class AssignExpertCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IOptimizationCaseRepository> _caseRepository = new();
    private readonly Mock<IIdentityServiceClient> _identityServiceClient = new();
    private readonly Mock<IAiServiceClient> _aiServiceClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();
    private readonly Mock<ILogger<AssignExpertCommandHandler>> _logger = new();

    private readonly AssignExpertCommandHandler _handler;

    public AssignExpertCommandHandlerTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);

        _publishEndpoint
            .Setup(p => p.Publish(
                It.IsAny<CaseAssignedEvent>(),
                It.IsAny<IPipe<PublishContext<CaseAssignedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AssignExpertCommandHandler(
            _caseRepository.Object,
            _identityServiceClient.Object,
            _aiServiceClient.Object,
            _unitOfWork.Object,
            _publishEndpoint.Object,
            _dateTimeProvider.Object,
            _logger.Object);
    }

    private static OptimizationCase MakeCase(SegmentType segment = SegmentType.RISKLI_KAYIP, CaseStatus status = CaseStatus.YENI) => new()
    {
        Id = Guid.NewGuid(),
        CaseNumber = "OPT-2026-000001",
        CampaignId = Guid.NewGuid(),
        Segment = segment,
        Priority = CasePriority.ORTA,
        Status = status,
        CreatedAt = Now.AddHours(-1),
    };

    private static IdentityExpertDto MakeExpert(SegmentType expertise) =>
        new(Guid.NewGuid(), "Ad", "Soyad", "MARMARA", new[] { expertise });

    [Fact]
    public async Task Vaka_YENI_disindaysa_atama_yapilmaz()
    {
        var optimizationCase = MakeCase(status: CaseStatus.ATANDI);
        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeFalse();
        _identityServiceClient.Verify(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Vaka_bulunamazsa_atama_yapilmaz()
    {
        _caseRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OptimizationCase?)null);

        var result = await _handler.Handle(new AssignExpertCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Segment_BELIRSIZ_ise_otomatik_atama_atlanir()
    {
        var optimizationCase = MakeCase(segment: SegmentType.BELIRSIZ);
        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeFalse();
        _identityServiceClient.Verify(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Segmente_uygun_uzman_yoksa_atama_yapilmaz()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { MakeExpert(SegmentType.YUKSEK_DEGER) }); // eslesme yok

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeFalse();
        _aiServiceClient.Verify(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Uygun_uzmanlarin_tumu_kapasite_doluysa_atama_yapilmaz()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var expert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { expert });
        // MaxActiveCasesPerExpert = 5 -> 5 aktif vaka = kapasite dolu (< 5 sarti saglanmiyor).
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(expert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeFalse();
        _aiServiceClient.Verify(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 1.0)]   // bos kapasite -> tam skor
    [InlineData(1, 0.8)]
    [InlineData(3, 0.4)]
    [InlineData(4, 0.2)]   // kapasiteye en yakin (5'ten 1 once) -> en dusuk ama pozitif skor
    public async Task PerformanceScore_formulu_aktif_vaka_sayisindan_dogru_turetilir(int activeCount, double expectedScoreRaw)
    {
        var expectedScore = (decimal)expectedScoreRaw;
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var expert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { expert });
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(expert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCount);

        AiAssignRequest? capturedRequest = null;
        _aiServiceClient
            .Setup(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AiAssignRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync((IReadOnlyList<AiAssignmentScoreDto>?)null);

        await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        var candidate = capturedRequest!.Candidates.Should().ContainSingle().Subject;
        candidate.ExpertId.Should().Be(expert.Id);
        candidate.PerformanceScore.Should().Be(expectedScore);
    }

    [Fact]
    public async Task AI_en_yuksek_skorlu_uygun_adayi_secer_ve_vakayi_ATANDI_yapar()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var lowScoreExpert = MakeExpert(SegmentType.RISKLI_KAYIP);
        var highScoreExpert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { lowScoreExpert, highScoreExpert });
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _aiServiceClient
            .Setup(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiAssignmentScoreDto>
            {
                new(lowScoreExpert.Id, 0.30m),
                new(highScoreExpert.Id, 0.92m),
            });

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeTrue();
        optimizationCase.Status.Should().Be(CaseStatus.ATANDI);
        optimizationCase.AssignedExpertId.Should().Be(highScoreExpert.Id);
        _caseRepository.Verify(r => r.AddStatusHistory(It.Is<CaseStatusHistory>(
            h => h.FromStatus == CaseStatus.YENI && h.ToStatus == CaseStatus.ATANDI)), Times.Once);
        _publishEndpoint.Verify(p => p.Publish(
            It.Is<CaseAssignedEvent>(e => e.Payload.ExpertId == highScoreExpert.Id && e.Payload.CaseId == optimizationCase.Id),
            It.IsAny<IPipe<PublishContext<CaseAssignedEvent>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AI_sadece_uygun_olmayan_aday_icin_skor_donerse_atama_yapilmaz()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var eligibleExpert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { eligibleExpert });
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(eligibleExpert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        // AI, aday listesinde olmayan bir uzman icin skor donuyor (ör. race condition/stale veri) - yok sayilmali.
        _aiServiceClient
            .Setup(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AiAssignmentScoreDto> { new(Guid.NewGuid(), 0.99m) });

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeFalse();
        optimizationCase.Status.Should().Be(CaseStatus.YENI);
    }

    [Fact]
    public async Task AI_null_donerse_en_bos_kapasiteli_uzmana_fallback_atama_yapilir()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var busyExpert = MakeExpert(SegmentType.RISKLI_KAYIP);
        var freeExpert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { busyExpert, freeExpert });
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(busyExpert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(freeExpert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _aiServiceClient
            .Setup(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AiAssignmentScoreDto>?)null);

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeTrue();
        optimizationCase.AssignedExpertId.Should().Be(freeExpert.Id, "AI kapaliyken en az yuklu (kapasitesi en bos) uzman secilmeli");
    }

    [Fact]
    public async Task AI_servisi_exception_firlatirsa_akis_kesilmez_fallback_devreye_girer()
    {
        var optimizationCase = MakeCase(segment: SegmentType.RISKLI_KAYIP);
        var onlyExpert = MakeExpert(SegmentType.RISKLI_KAYIP);

        _caseRepository.Setup(r => r.GetByIdAsync(optimizationCase.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(optimizationCase);
        _identityServiceClient.Setup(c => c.GetExpertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { onlyExpert });
        _caseRepository.Setup(r => r.CountActiveByExpertAsync(onlyExpert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _aiServiceClient
            .Setup(c => c.AssignAsync(It.IsAny<AiAssignRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("AI servisi kapali"));

        var result = await _handler.Handle(new AssignExpertCommand(optimizationCase.Id), CancellationToken.None);

        result.Should().BeTrue("AI cagrisi patlasa bile atama akisi (demo sigortasi) kesilmemeli");
        optimizationCase.AssignedExpertId.Should().Be(onlyExpert.Id);
    }
}
