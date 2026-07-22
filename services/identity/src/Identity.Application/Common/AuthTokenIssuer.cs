using System.Security.Cryptography;
using Identity.Application.Dtos;
using Identity.Domain.Entities;

namespace Identity.Application.Common;

public sealed record TokenIssuanceResult(AuthResultDto AuthResult, RefreshToken RefreshTokenEntity);

/// <summary>
/// VerifyOtp/Login/RefreshToken handler'larının ortak token üretim mantığı — tek yerde,
/// tutarlı (Core_Principles §6: access 15 dk, refresh 7 gün, refresh SHA-256 hash'lenir).
/// </summary>
public sealed class AuthTokenIssuer
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthTokenIssuer(IJwtTokenGenerator jwtTokenGenerator, ITokenHasher tokenHasher, IDateTimeProvider dateTimeProvider)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public TokenIssuanceResult IssueFor(User user, string ipAddress)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var plainRefreshToken = GenerateSecureToken();
        var refreshTokenHash = _tokenHasher.Sha256(plainRefreshToken);
        var refreshTokenEntity = user.IssueRefreshToken(refreshTokenHash, _dateTimeProvider.UtcNow, ipAddress);

        var expiresInSeconds = (int)(accessToken.ExpiresAtUtc - _dateTimeProvider.UtcNow).TotalSeconds;
        var authResult = new AuthResultDto(accessToken.Token, plainRefreshToken, expiresInSeconds, user.ToSummaryDto());

        return new TokenIssuanceResult(authResult, refreshTokenEntity);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
