using BuildingBlocks.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Identity.Api.Security;

/// <summary>
/// Core_Principles §6: "Servisler arasi sistem cagrilari: X-Internal-Api-Key header".
/// GetExperts gibi servisler-arasi (Campaign -> Identity) endpoint'lerde JWT yerine
/// bu filtre kullanilir — cagiran taraf Gateway'i atlayip container network'unden
/// dogrudan cagirir, dolayisiyla kullanici JWT'si yoktur.
/// </summary>
public sealed class InternalApiKeyFilter : IAsyncActionFilter
{
    private const string HeaderName = "X-Internal-Api-Key";
    private readonly IConfiguration _configuration;

    public InternalApiKeyFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expectedKey = _configuration["INTERNAL_API_KEY"];
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expectedKey) || string.IsNullOrEmpty(providedKey) || providedKey != expectedKey)
        {
            throw new InvalidCredentialsException("INTERNAL_API_KEY_INVALID", "Gecersiz veya eksik internal API anahtari.");
        }

        await next();
    }
}
