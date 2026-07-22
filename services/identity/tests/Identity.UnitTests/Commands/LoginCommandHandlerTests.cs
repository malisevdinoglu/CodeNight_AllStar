using BuildingBlocks.Exceptions;
using FluentAssertions;
using Identity.Application.Commands.Login;
using Identity.Application.Common;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.UnitTests.Commands;

/// <summary>Core_Principles §10: 5 hatalı denemede 423 + remainingSeconds; her deneme audit log.</summary>
public sealed class LoginCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly Mock<ITokenHasher> _tokenHasher = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _dateTimeProvider.Setup(p => p.UtcNow).Returns(Now);
        _requestContext.Setup(c => c.IpAddress).Returns("127.0.0.1");
        _tokenHasher.Setup(h => h.Sha256(It.IsAny<string>())).Returns("hash");
        _jwtTokenGenerator
            .Setup(g => g.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AccessTokenResult("access-token", Guid.NewGuid().ToString(), Now.AddMinutes(15)));

        var authTokenIssuer = new AuthTokenIssuer(_jwtTokenGenerator.Object, _tokenHasher.Object, _dateTimeProvider.Object);

        _handler = new LoginCommandHandler(
            _userRepository.Object,
            _auditLogRepository.Object,
            _unitOfWork.Object,
            _passwordHasher.Object,
            authTokenIssuer,
            _requestContext.Object,
            _dateTimeProvider.Object);
    }

    private static User CreateStaffUser() =>
        User.CreateStaff("Mert", "Ozturk", "mert@campaigncell.com", "bcrypt-hash", Role.PERSONEL, "MARMARA", Array.Empty<SegmentType>());

    [Fact]
    public async Task Besinci_hatali_sifre_denemesinde_423_ve_kalan_sure_doner()
    {
        var user = CreateStaffUser();
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Ilk 4 deneme: sadece INVALID_CREDENTIALS (henuz kilitlenmez).
        for (var i = 0; i < User.MaxFailedLoginAttempts - 1; i++)
        {
            var earlyAct = async () => await _handler.Handle(new LoginCommand(user.Email!, "yanlis-sifre"), CancellationToken.None);
            await earlyAct.Should().ThrowAsync<InvalidCredentialsException>();
        }

        // 5. deneme: kilitlenir.
        var act = async () => await _handler.Handle(new LoginCommand(user.Email!, "yanlis-sifre"), CancellationToken.None);
        var exception = await act.Should().ThrowAsync<AccountLockedException>();
        exception.Which.RemainingSeconds.Should().Be(User.LockDurationMinutes * 60);
        exception.Which.StatusCode.Should().Be(423);

        _jwtTokenGenerator.Verify(g => g.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Kilitliyken_dogru_sifre_ile_bile_giris_reddedilir_ve_sifre_kontrol_edilmez()
    {
        var user = CreateStaffUser();
        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = async () => await _handler.Handle(new LoginCommand(user.Email!, "dogru-sifre-olsa-bile"), CancellationToken.None);

        await act.Should().ThrowAsync<AccountLockedException>();
        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never,
            "kilitliyken sifre HIC kontrol edilmemeli (Core_Principles §10)");
    }

    [Fact]
    public async Task Basarili_giris_sayaci_sifirlar_ve_token_cifti_doner()
    {
        var user = CreateStaffUser();
        user.RegisterFailedLogin(Now.AddMinutes(-1));
        user.RegisterFailedLogin(Now.AddMinutes(-1));

        _userRepository.Setup(r => r.GetByEmailAsync(user.Email!, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _handler.Handle(new LoginCommand(user.Email!, "dogru-sifre"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        user.FailedLoginCount.Should().Be(0);
    }
}
