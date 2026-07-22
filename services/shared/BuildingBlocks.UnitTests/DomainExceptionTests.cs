using BuildingBlocks.Exceptions;
using FluentAssertions;
using Xunit;

namespace BuildingBlocks.UnitTests;

public class DomainExceptionTests
{
    [Fact]
    public void DomainRuleException_varsayilan_statu_kodu_422_olmali()
    {
        var exception = new DomainRuleException("INVALID_TRANSITION", "Gecersiz durum gecisi.");

        exception.StatusCode.Should().Be(422);
        exception.ErrorCode.Should().Be("INVALID_TRANSITION");
        exception.Details.Should().BeEmpty();
    }

    [Fact]
    public void ConflictException_statu_kodu_409_olmali()
    {
        var exception = new ConflictException("ALREADY_RATED", "Bu teklif zaten puanlandi.");

        exception.StatusCode.Should().Be(409);
    }

    [Fact]
    public void DomainException_details_verilirse_korumali()
    {
        var exception = new DomainRuleException(
            "PASSWORD_POLICY",
            "Sifre politikasi ihlali.",
            new[] { "En az 1 buyuk harf icermelidir", "En az 1 rakam icermelidir" });

        exception.Details.Should().HaveCount(2);
    }

    [Fact]
    public void AccountLockedException_423_ve_remainingSeconds_tasimali()
    {
        var exception = new AccountLockedException(842);

        exception.StatusCode.Should().Be(423);
        exception.ResponseData.Should().BeEquivalentTo(new { remainingSeconds = 842 });
    }

    [Fact]
    public void InvalidCredentialsException_401_olmali()
    {
        var exception = new InvalidCredentialsException("INVALID_CREDENTIALS", "Gecersiz kimlik bilgileri.");

        exception.StatusCode.Should().Be(401);
    }

    [Fact]
    public void NotFoundException_404_olmali()
    {
        var exception = new NotFoundException("USER_NOT_FOUND", "Kullanici bulunamadi.");

        exception.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ForbiddenException_403_olmali()
    {
        var exception = new ForbiddenException("ROLE_NOT_ALLOWED", "Bu islem icin yetkiniz yok.");

        exception.StatusCode.Should().Be(403);
    }
}
