using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Behaviors;

/// <summary>
/// Her MediatR isteğini yapılandırılmış (structured) şekilde loglar: başlangıç,
/// süre, ve varsa hata. Serilog sink'i Program.cs'te ayrıca yapılandırılır
/// (Core_Principles §1: "Serilog, structured, JSON"); bu sınıf sadece
/// Microsoft.Extensions.Logging soyutlamasına bağımlıdır.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Islem basladi: {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation(
                "Islem tamamlandi: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "Islem hata ile sonuclandi: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
