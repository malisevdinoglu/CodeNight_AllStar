using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Common;
using Campaign.Application.External;
using Campaign.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Campaign.Infrastructure.External;

/// <summary>
/// Core_Principles §6: Gateway atlanır, container ağından "http://identity-api:8080" ile
/// doğrudan çağrılır, X-Internal-Api-Key ile korunur. Identity.Api.UsersController.GetExperts
/// bunun karşılığıdır. Interface sözleşmesi non-nullable IReadOnlyList döndürdüğü için (AI'nin
/// aksine) çökme durumunda boş liste graceful degradation sinyalidir - caller'lar zaten
/// "uzman bulunamadı" durumunu ele almak zorunda (manuel kuyruk vb.).
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

    public async Task<IReadOnlyList<IdentityExpertDto>> GetExpertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<ExpertWireDto>>>(
                "api/v1/users/experts", WireOptions, cancellationToken);

            if (response?.Data is null)
            {
                return Array.Empty<IdentityExpertDto>();
            }

            return response.Data.Select(ToApplicationDto).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Identity /api/v1/users/experts cagrisi basarisiz oldu, bos liste donuluyor.");
            return Array.Empty<IdentityExpertDto>();
        }
    }

    private static IdentityExpertDto ToApplicationDto(ExpertWireDto wire) => new(
        wire.Id,
        wire.FirstName,
        wire.LastName,
        wire.Region,
        wire.Expertise
            .Select(e => Enum.TryParse<SegmentType>(e, out var segment) ? segment : (SegmentType?)null)
            .Where(s => s is not null)
            .Select(s => s!.Value)
            .ToList());

    private sealed record ExpertWireDto(Guid Id, string FirstName, string LastName, string? Region, IReadOnlyList<string> Expertise);
}
