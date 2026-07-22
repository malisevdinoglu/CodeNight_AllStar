using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Core_Principles §6 payload sözleşmesi: sub, role, expertise[], region, jti, exp.
/// Claim isimleri Gateway'in beklediği ("sub"/"role"/"expertise") ile BİREBİR aynı —
/// DefaultInboundClaimTypeMap Gateway'de zaten temizlendiği için burada da ham isim kullanılır.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult GenerateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("role", user.Role.ToString()),
            new("jti", jti),
        };

        if (!string.IsNullOrWhiteSpace(user.Region))
        {
            claims.Add(new Claim("region", user.Region));
        }

        claims.AddRange(user.Expertises.Select(e => new Claim("expertise", e.SegmentType.ToString())));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResult(tokenString, jti, expiresAt);
    }
}
