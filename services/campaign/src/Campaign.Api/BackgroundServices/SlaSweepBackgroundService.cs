using System.Threading;
using BuildingBlocks.Messaging;
using Campaign.Application.Common;
using Campaign.Application.Events;
using MassTransit;

namespace Campaign.Api.BackgroundServices;

/// <summary>
/// Mali_Plan.md: "SLA Worker (dakikalık BackgroundService)". Aktif (terminal olmayan) ve
/// deadline'ı geçmiş, henüz işaretlenmemiş vakaları tarar; SlaBreached=true yapar ve
/// case.sla_breached yayınlar (Gamification consumer'ı dinler: -5 puan).
/// </summary>
public sealed class SlaSweepBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaSweepBackgroundService> _logger;

    public SlaSweepBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SlaSweepBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ilk taramadan once containerlarin/DB migration'in oturmasi icin kisa bir bekleme.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(SweepInterval);
        do
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SLA sweep sirasinda beklenmeyen hata.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var caseRepository = scope.ServiceProvider.GetRequiredService<IOptimizationCaseRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = dateTimeProvider.UtcNow;
        var breachedCases = await caseRepository.GetActiveForSlaSweepAsync(now, cancellationToken);
        if (breachedCases.Count == 0)
        {
            return;
        }

        foreach (var optimizationCase in breachedCases)
        {
            optimizationCase.SlaBreached = true;

            await publishEndpoint.PublishIntegrationEventAsync(
                new CaseSlaBreachedEvent
                {
                    Timestamp = now.UtcDateTime,
                    Payload = new CaseSlaBreachedPayload(
                        optimizationCase.Id, optimizationCase.AssignedExpertId,
                        optimizationCase.Priority.ToString(), now),
                },
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("SLA sweep: {Count} vaka ihlal olarak isaretlendi.", breachedCases.Count);
    }
}
