using BuildingBlocks.Exceptions;
using FluentAssertions;
using Identity.Application.Commands.RefreshToken;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.UnitTests.Commands;

/// <summary>
/// Mali.md §4: "Bu akışa unit test yaz — jüri bunu canlı deneyecek." Refresh token
/// rotation + theft koruması (Core_Principles §10) — case'in en kritik güvenlik akışı.
/// </summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITokenHasher> _tokenHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly AuthTokenIssuer _authTokenIssuer;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
        _tokenHasher.Setup(h => h.Sha256(It.IsAny<string>())).Returns((string plain) => $"hash-of-{plain}");
        _jwtTokenGenerator
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AccessTokenResult("access-token", Guid.NewGuid().ToString(), Now.AddMinutes(15)));
        _requestContext.Setup(c => c.IpAddress).Returns("127.0.0.1");

        _authTokenIssuer = new AuthTokenIssuer(_jwtTokenGenerator.Object, _tokenHasher.Object, _dateTimeProvider.Object);

        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepository.Object,
            _userRepository.Object,
            _auditLogRepository.Object,
            _unitOfWork.Object,
            _tokenHasher.Object,
            _authTokenIssuer,
            _requestContext.Object,
            _dateTimeProvider.Object);
    }

    private static User CreateStaffUser() =>
        User.CreateStaff("Ayse", "Yilmaz", "ayse@campaigncell.com", "hashed", Role.PERSONEL, "MARMARA", Array.Empty<SegmentType>());

    [Fact]
    public async Task Gecerli_aktif_token_rotation_yapar_ve_eskiyi_iptal_eder()
    {
        var user = CreateStaffUser();
        var oldToken = user.IssueRefreshToken("hash-of-plain-refresh-token", Now.AddDays(-1), "127.0.0.1");

        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hash-of-plain-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);
        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new RefreshTokenCommand("plain-refresh-token"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        oldToken.RevokedAt.Should().Be(Now);
        oldToken.ReplacedById.Should().NotBeNull();
        user.RefreshTokens.Should().HaveCount(2, "rotation yeni bir refresh token eklemiş olmalı");

        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a =>
            a.ActionType == AuditActionType.TOKEN_REFRESHED && a.Success)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Iptal_edilmis_token_tekrar_kullanilirsa_theft_algilanir_ve_tum_oturumlar_kapatilir()
    {
        var user = CreateStaffUser();
        var stolenToken = user.IssueRefreshToken("hash-of-plain-refresh-token", Now.AddDays(-2), "127.0.0.1");
        var secondActiveToken = user.IssueRefreshToken("hash-of-other-token", Now.AddDays(-1), "10.0.0.5");
        stolenToken.RevokeAndReplace(Now.AddHours(-1), secondActiveToken.Id); // daha once rotate edilmis (calinip tekrar kullaniliyor)

        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hash-of-plain-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stolenToken);
        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _handler.Handle(new RefreshTokenCommand("plain-refresh-token"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
        exception.Which.ErrorCode.Should().Be("REFRESH_TOKEN_REUSED");

        secondActiveToken.RevokedAt.Should().Be(Now, "theft tespitinde kullanicinin TUM aktif oturumlari kapatilmali");
        secondActiveToken.IsActive(Now).Should().BeFalse();

        _auditLogRepository.Verify(r => r.Add(It.Is<AuditLog>(a =>
            a.ActionType == AuditActionType.TOKEN_THEFT_DETECTED && !a.Success)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Yeni token asla verilmemeli.
        _jwtTokenGenerator.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Suresi_dolmus_token_reddedilir()
    {
        var user = CreateStaffUser();
        var expiredToken = user.IssueRefreshToken("hash-of-plain-refresh-token", Now.AddDays(-10), "127.0.0.1");
        // ExpiryDays = 7, Now.AddDays(-10) + 7 gun < Now -> expired.

        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync("hash-of-plain-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);
        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = async () => await _handler.Handle(new RefreshTokenCommand("plain-refresh-token"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
        exception.Which.ErrorCode.Should().Be("REFRESH_TOKEN_EXPIRED");
    }

    [Fact]
    public async Task Bilinmeyen_token_hash_gecersiz_kimlik_bilgisi_hatasi_dondurur()
    {
        _refreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = async () => await _handler.Handle(new RefreshTokenCommand("bilinmeyen-token"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<InvalidCredentialsException>();
        exception.Which.ErrorCode.Should().Be("REFRESH_TOKEN_INVALID");
    }
}
