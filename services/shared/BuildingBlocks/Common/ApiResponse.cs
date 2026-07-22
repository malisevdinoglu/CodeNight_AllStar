using System.Text.Json.Serialization;

namespace BuildingBlocks.Common;

/// <summary>
/// Dört serviste de birebir aynı standart API zarfı (Core_Principles §5).
/// Başarı ve hata durumunda AYNI şekil kullanılır.
/// </summary>
public record ApiResponse<T>
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public ApiError? Error { get; init; }
}

/// <summary>
/// Hata kodu formatı: {SERVIS}_{HTTP}_{SEBEP} (Core_Principles §5), örn. AUTH_423_ACCOUNT_LOCKED.
/// </summary>
public record ApiError
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("details")]
    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Sayfalama sözleşmesi (Core_Principles §5): ?page=1&amp;pageSize=20 →
/// data: { items, page, pageSize, totalCount }.
/// </summary>
public record PagedResult<T>
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<T> Items { get; init; }

    [JsonPropertyName("page")]
    public required int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }
}
