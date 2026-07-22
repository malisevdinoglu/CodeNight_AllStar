using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Common;
using Gamification.Application.External;
using Microsoft.Extensions.Logging;

namespace Gamification.Infrastructure.External;

/// <summary>
/// Campaign.Infrastructure.External.IdentityServiceClient ile aynı desen (Core_Principles §6):
/// Gateway atlanır, container ağından "http://identity-api:8080" ile X-Internal-Api-Key
/// korumalı çağrılır. Sadece görünen ad (display name) çözümü için kullanılır — puanlama/rozet
/// mantığı bu servise ASLA senkron bağımlı değildir (Process* handler'ları çağırmaz).
/// </summary>
public sealed class IdentityServiceClient : IIdentityServiceClient
{
    private static readonly JsonSerializerOptions WireOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityServiceClient> _logger;

    public IdentityServiceClient(HttpClient httpClient, ILogger<IdentityServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IdentityUserDto>> GetExpertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ExpertWireDto>>>(
                "api/v1/users/experts", WireOptions, cancellationToken);

            if (response?.Data is null)
            {
                return Array.Empty<IdentityUserDto>();
            }

            return response.Data.Select(w => new IdentityUserDto(w.Id, w.FirstName, w.LastName)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Identity /api/v1/users/experts cagrisi basarisiz oldu, bos liste donuluyor.");
            return Array.Empty<IdentityUserDto>();
        }
    }

    private sealed record ExpertWireDto(Guid Id, string FirstName, string LastName);
}
