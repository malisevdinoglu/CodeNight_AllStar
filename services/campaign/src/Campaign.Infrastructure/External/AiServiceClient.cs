using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Common;
using Campaign.Application.External;
using Microsoft.Extensions.Logging;

namespace Campaign.Infrastructure.External;

/// <summary>
/// Core_Principles §3 graceful degradation'ın somut uygulaması: HttpClient.Timeout DI'da 3 sn'ye
/// sabitlenir (bkz. InfrastructureServiceCollectionExtensions), her metot try/catch içinde -
/// AĞ hatası, timeout, deserialize hatası, 4xx/5xx FARK ETMEKSİZİN null döner, exception asla
/// çağırana sızmaz (interface sözleşmesi - "demo adım 7 sigortası" burada gerçekleşir).
/// </summary>
public sealed class AiServiceClient : IAiServiceClient
{
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<AiServiceClient> _logger;

    public AiServiceClient(HttpClient httpClient, ILogger<AiServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<IReadOnlyList<AiRecommendationDto>?> RecommendAsync(
        AiRecommendRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AiRecommendRequest, IReadOnlyList<AiRecommendationDto>>("api/v1/ai/recommend", request, cancellationToken);

    public Task<AiClassifyResult?> ClassifyAsync(
        AiClassifyRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AiClassifyRequest, AiClassifyResult>("api/v1/ai/classify", request, cancellationToken);

    public Task<IReadOnlyList<AiAssignmentScoreDto>?> AssignAsync(
        AiAssignRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<AiAssignRequest, IReadOnlyList<AiAssignmentScoreDto>>("api/v1/ai/assign", request, cancellationToken);

    public async Task<AiAccuracyMetricsDto?> GetAccuracyMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<AiAccuracyMetricsDto>>(
                "api/v1/ai/metrics/accuracy", WireOptions, cancellationToken);
            return response?.Data;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI /api/v1/ai/metrics/accuracy cagrisi basarisiz oldu, null donuluyor.");
            return null;
        }
    }

    private async Task<TResult?> PostAsync<TRequest, TResult>(string path, TRequest body, CancellationToken cancellationToken)
    {
        try
        {
            using var httpResponse = await _httpClient.PostAsJsonAsync(path, body, WireOptions, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<TResult>>(WireOptions, cancellationToken);
            return response is { Success: true } ? response.Data : default;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // AI kapali/zaman asimi/beklenmeyen govde - graceful degradation: null don, asla firlatma.
            _logger.LogWarning(ex, "AI servisi cagrisi ({Path}) basarisiz oldu, null donuluyor.", path);
            return default;
        }
    }
}
