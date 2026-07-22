namespace Identity.Application.Dtos;

/// <summary>Login/VerifyOtp/RefreshToken ortak dönüş şekli — REST alanları camelCase (Core_Principles §4).</summary>
public sealed record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    UserSummaryDto User);
