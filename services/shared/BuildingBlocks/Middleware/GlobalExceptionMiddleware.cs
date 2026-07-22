using System.Text.Json;
using BuildingBlocks.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Middleware;

/// <summary>
/// Tek merkezi hata yakalayıcı (Core_Principles §5). Sıra:
/// FluentValidation.ValidationException → 400, DomainException → 422/409 (ErrorCode taşır),
/// diğer her şey → 500 (stack trace ASLA client'a sızmaz, sadece loglanır).
/// Hata kodu formatı: {SERVIS}_{HTTP}_{SEBEP} — servisPrefix her Api'nin Program.cs'inde verilir
/// (örn. "AUTH", "CMP", "GAM") çünkü bu bilgi BuildingBlocks'un bilemeyeceği bir servis detayıdır.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly string _servicePrefix;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, string servicePrefix)
    {
        _next = next;
        _logger = logger;
        _servicePrefix = servicePrefix;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            var details = validationException.Errors.Select(e => e.ErrorMessage).ToArray();
            _logger.LogInformation("Dogrulama hatasi: {Details}", string.Join(" | ", details));
            await WriteErrorAsync(context, 400,
                code: $"{_servicePrefix}_400_VALIDATION",
                message: "Dogrulama hatasi.",
                details: details);
        }
        catch (DomainException domainException)
        {
            _logger.LogWarning(domainException, "Domain kurali ihlali: {ErrorCode}", domainException.ErrorCode);
            await WriteErrorAsync(context, domainException.StatusCode,
                code: $"{_servicePrefix}_{domainException.StatusCode}_{domainException.ErrorCode}",
                message: domainException.Message,
                details: domainException.Details,
                data: domainException.ResponseData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata");
            await WriteErrorAsync(context, 500,
                code: $"{_servicePrefix}_500_INTERNAL_ERROR",
                message: "Beklenmeyen bir hata olustu.",
                details: Array.Empty<string>()); // stack trace ASLA client'a sizmaz
        }
    }

    private static Task WriteErrorAsync(
        HttpContext context, int statusCode, string code, string message,
        IReadOnlyList<string> details, object? data = null)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            success = false,
            data,
            error = new { code, message, details }
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Her Api'nin Program.cs'inde en başta çağrılır: <c>app.UseCampaignCellExceptionHandling("AUTH");</c>
    /// </summary>
    public static IApplicationBuilder UseCampaignCellExceptionHandling(this IApplicationBuilder app, string servicePrefix)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>(servicePrefix);
    }
}
