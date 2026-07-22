using BuildingBlocks.Behaviors;
using Campaign.Application;
using Campaign.Application.Commands.CreateCampaign;
using Campaign.Application.Common;
using Campaign.Application.External;
using Campaign.Domain.Entities;
using Campaign.Domain.Enums;
using Campaign.Infrastructure.Extensions;
using Campaign.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Campaign.UnitTests.Integration;

/// <summary>
/// Mali.md §9: "En az 1 integration test: CreateCampaign → AI mock → Offer üretimi."
/// Unit testlerden farkı: burada repository'ler MOCK DEĞİL — gerçek EF Core (InMemory
/// provider) DbContext'i, gerçek MediatR pipeline'ı (LoggingBehavior + ValidationBehavior +
/// FluentValidation) ve gerçek AssignExpertCommand zincirlemesi üzerinden çalışır. Yalnızca
/// dış sınır (AI servisi, Identity servisi, MassTransit publish) sahte/mock — IAiServiceClient
/// arayüzünün "her çağrı graceful-degrade eder, exception fırlatmaz" sözleşmesi gereği "AI mock"
/// tam olarak budur (Core_Principles §3).
/// NOT: Bu proje bu sandbox'ta derlenemedi (.NET SDK yok) — `dotnet test` ile doğrulanmalı.
/// </summary>
public sealed class CreateCampaignIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;

    public CreateCampaignIntegrationTests()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<CampaignDbContext>(options =>
            options.UseInMemoryDatabase(dbName).UseSnakeCaseNamingConvention());

        // Gercek repository/UnitOfWork implementasyonlari (mock DEGIL) - production DI ile birebir.
        services.AddCampaignInfrastructure(configuration);
        services.AddHttpContextAccessor();

        // Sadece dis sinir sahte: AI ve Identity servisleri gercek HTTP yerine kontrollu fake'lerle
        // degistirilir (son kayit kazanir - AddCampaignInfrastructure'daki AddHttpClient kayitlarinin
        // uzerine yazar). MassTransit/RabbitMQ hic devreye girmez - IPublishEndpoint loose mock'lanir
        // (Moq, Task donen unconfigured metotlarda varsayilan olarak Task.CompletedTask dondugu icin
        // ayrica Setup gerekmez - RefreshTokenCommandHandlerTests'teki gibi elle Setup etmeye gerek yok).
        // INumberSequenceProvider da fake'lenir: gercek implementasyon Postgres'e ozel
        // "SELECT nextval(...)" ham SQL'i calistirir, InMemory provider bunu desteklemez.
        services.AddSingleton<IAiServiceClient>(new FakeAiServiceClient());
        services.AddSingleton<IIdentityServiceClient>(new FakeIdentityServiceClient());
        services.AddSingleton(new Mock<IPublishEndpoint>().Object);
        services.AddSingleton<ICurrentRequestContext>(new FakeCurrentRequestContext(Guid.NewGuid()));
        services.AddSingleton<INumberSequenceProvider, FakeNumberSequenceProvider>();

        services.AddLogging();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
    }

    private CampaignDbContext Db => _scope.ServiceProvider.GetRequiredService<CampaignDbContext>();

    [Fact]
    public async Task CreateCampaign_AI_skor_esigin_ustunde_donerse_Offer_uretilir_ve_DBye_yazilir()
    {
        // Arrange: hedef segmentte 1 abone, gercek DbContext'e seed edilir.
        var subscriberId = Guid.NewGuid();
        Db.SubscriberProfiles.Add(new SubscriberProfile
        {
            SubscriberId = subscriberId,
            GsmNumber = "5551234567",
            CurrentPlan = "Standart",
            TenureMonths = 18,
            AvgMonthlyDataGb = 12m,
            AvgMonthlyCallMinutes = 400,
            MonthlySpendTl = 180m,
            PackagePurchaseCount = 2,
            ComplaintCount = 0,
            DaysSinceLastActivity = 1,
            PastAcceptanceRate = 0.5m,
            CurrentSegment = SegmentType.YUKSEK_DEGER,
        });
        await Db.SaveChangesAsync();

        var mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCampaignCommand("Entegrasyon Test Kampanyasi", CampaignType.EK_PAKET, SegmentType.YUKSEK_DEGER, "aciklama");

        // Act: gercek MediatR pipeline -> gercek handler -> gercek EF repository'ler -> AI mock.
        var result = await mediator.Send(command);

        // Assert: handler'in donus degeri.
        result.AiAvailable.Should().BeTrue();
        result.PredictedSegment.Should().Be(SegmentType.YUKSEK_DEGER);
        result.CampaignNumber.Should().NotBeNullOrWhiteSpace();
        result.CaseNumber.Should().NotBeNullOrWhiteSpace();

        // Assert: veri GERCEKTEN veritabanina yazilmis (mock repository degil, gercek EF sorgusu).
        var persistedCampaign = await Db.Campaigns.FirstOrDefaultAsync(c => c.Id == result.CampaignId);
        persistedCampaign.Should().NotBeNull();
        persistedCampaign!.Title.Should().Be("Entegrasyon Test Kampanyasi");

        var persistedCase = await Db.OptimizationCases.FirstOrDefaultAsync(c => c.Id == result.CaseId);
        persistedCase.Should().NotBeNull();
        persistedCase!.Status.Should().Be(CaseStatus.YENI, "uzman havuzu bos oldugu icin otomatik atama gerceklesmemeli");

        var persistedOffer = await Db.Offers.FirstOrDefaultAsync(o => o.CampaignId == result.CampaignId);
        persistedOffer.Should().NotBeNull("AI skoru esigin (0.60) uzerinde oldugu icin bir teklif uretilmis olmali");
        persistedOffer!.SubscriberId.Should().Be(subscriberId);
        persistedOffer.RecommendationScore.Should().Be(0.85m);
    }

    [Fact]
    public async Task CreateCampaign_AI_null_donerse_kampanya_yine_olusur_ama_Offer_uretilmez()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<CampaignDbContext>(options =>
            options.UseInMemoryDatabase(dbName).UseSnakeCaseNamingConvention());
        services.AddCampaignInfrastructure(configuration);
        services.AddHttpContextAccessor();
        services.AddSingleton<IAiServiceClient>(new FakeAiServiceClient(aiIsDown: true));
        services.AddSingleton<IIdentityServiceClient>(new FakeIdentityServiceClient());
        services.AddSingleton(new Mock<IPublishEndpoint>().Object);
        services.AddSingleton<ICurrentRequestContext>(new FakeCurrentRequestContext(Guid.NewGuid()));
        services.AddSingleton<INumberSequenceProvider, FakeNumberSequenceProvider>();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();

        var subscriberId = Guid.NewGuid();
        db.SubscriberProfiles.Add(new SubscriberProfile
        {
            SubscriberId = subscriberId,
            GsmNumber = "5559876543",
            CurrentPlan = "Standart",
            TenureMonths = 6,
            AvgMonthlyDataGb = 5m,
            AvgMonthlyCallMinutes = 200,
            MonthlySpendTl = 90m,
            PackagePurchaseCount = 0,
            ComplaintCount = 1,
            DaysSinceLastActivity = 10,
            PastAcceptanceRate = 0.1m,
            CurrentSegment = SegmentType.YUKSEK_DEGER,
        });
        await db.SaveChangesAsync();

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var command = new CreateCampaignCommand("AI Kapali Test", CampaignType.EK_PAKET, SegmentType.YUKSEK_DEGER, null);

        var result = await mediator.Send(command);

        result.AiAvailable.Should().BeFalse();
        result.PredictedSegment.Should().Be(SegmentType.BELIRSIZ, "demo adim 7 sigortasi: AI kapaliyken BELIRSIZ fallback");

        var persistedCampaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == result.CampaignId);
        persistedCampaign.Should().NotBeNull("AI kapali olsa bile kampanya YINE olusturulmali");

        var offerCount = await db.Offers.CountAsync(o => o.CampaignId == result.CampaignId);
        offerCount.Should().Be(0, "AI kapaliyken hicbir teklif olusturulmamali");
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }

    private sealed class FakeCurrentRequestContext : ICurrentRequestContext
    {
        public FakeCurrentRequestContext(Guid userId) => UserId = userId;
        public Guid? UserId { get; }
        public string? Role => "SUPERVIZOR";
        public IReadOnlyList<string> Expertise => Array.Empty<string>();
        public string IpAddress => "127.0.0.1";
    }

    /// <summary>
    /// Core_Principles §3 sözleşmesine uyan sahte AI istemcisi: skor >= 0.60 ise teklif üretecek
    /// bir öneri döner (0.85 -> isPriority de tetiklenir çünkü > 0.80); aiIsDown=true ise
    /// RecommendAsync null döner (gerçek AiServiceClient'ın timeout/hata durumunda yaptığı gibi).
    /// </summary>
    private sealed class FakeAiServiceClient : IAiServiceClient
    {
        private readonly bool _aiIsDown;

        public FakeAiServiceClient(bool aiIsDown = false) => _aiIsDown = aiIsDown;

        public Task<IReadOnlyList<AiRecommendationDto>?> RecommendAsync(
            AiRecommendRequest request, CancellationToken cancellationToken = default)
        {
            if (_aiIsDown)
            {
                return Task.FromResult<IReadOnlyList<AiRecommendationDto>?>(null);
            }

            var campaignId = request.Campaigns[0].CampaignId;
            IReadOnlyList<AiRecommendationDto> recommendations = new List<AiRecommendationDto>
            {
                new(campaignId, 0.85m, 0.55m),
            };
            return Task.FromResult<IReadOnlyList<AiRecommendationDto>?>(recommendations);
        }

        public Task<AiClassifyResult?> ClassifyAsync(
            AiClassifyRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<AiClassifyResult?>(null);

        public Task<IReadOnlyList<AiAssignmentScoreDto>?> AssignAsync(
            AiAssignRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AiAssignmentScoreDto>?>(null);

        public Task<AiAccuracyMetricsDto?> GetAccuracyMetricsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<AiAccuracyMetricsDto?>(null);
    }

    /// <summary>Bos uzman havuzu döner - AssignExpertCommand dogal olarak no-op kalir (case YENI kalir).</summary>
    private sealed class FakeIdentityServiceClient : IIdentityServiceClient
    {
        public Task<IReadOnlyList<IdentityExpertDto>> GetExpertsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IdentityExpertDto>>(Array.Empty<IdentityExpertDto>());
    }

    /// <summary>
    /// Gercek CampaignNumberSequenceProvider Postgres'e ozel "nextval(sequence)" ham SQL'i
    /// calistirir - InMemory provider'da desteklenmez. Bu fake, ayni format sozlesmesini
    /// (NumberFormatter) koruyarak bellek ici, thread-safe bir sayac kullanir.
    /// </summary>
    private sealed class FakeNumberSequenceProvider : INumberSequenceProvider
    {
        private long _campaignSeq;
        private long _caseSeq;

        public Task<string> NextCampaignNumberAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Campaign.Domain.Services.NumberFormatter.FormatCampaignNumber(
                2026, Interlocked.Increment(ref _campaignSeq)));

        public Task<string> NextCaseNumberAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Campaign.Domain.Services.NumberFormatter.FormatCaseNumber(
                2026, Interlocked.Increment(ref _caseSeq)));
    }
}
