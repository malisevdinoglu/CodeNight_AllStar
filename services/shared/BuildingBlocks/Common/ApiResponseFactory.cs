namespace BuildingBlocks.Common;

/// <summary>
/// ApiResponse&lt;T&gt; üretimi için tek giriş noktası — controller/handler'larda
/// elle <c>new ApiResponse{...}</c> yazmak yerine bunu kullan (tutarlılık).
/// </summary>
public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data) =>
        new() { Success = true, Data = data, Error = null };

    public static ApiResponse<object?> SuccessEmpty() =>
        new() { Success = true, Data = null, Error = null };

    public static ApiResponse<T> Failure<T>(string code, string message, IReadOnlyList<string>? details = null) =>
        new()
        {
            Success = false,
            Data = default,
            Error = new ApiError { Code = code, Message = message, Details = details ?? Array.Empty<string>() }
        };
}
