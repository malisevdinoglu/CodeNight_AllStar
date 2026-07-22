using FluentAssertions;
using FluentValidation.TestHelper;
using Identity.Application.Commands.CreateStaff;
using Identity.Domain.Enums;
using Xunit;

namespace Identity.UnitTests.Commands;

/// <summary>
/// Case şartı: "hangi kural ihlal edildiği belli olacak" — her şifre kuralı ayrı RuleFor
/// bloğunda olduğu için, ihlal edilen HER kural aynı anda kendi mesajıyla dönmeli.
/// </summary>
public sealed class CreateStaffCommandValidatorTests
{
    private readonly CreateStaffCommandValidator _validator = new();

    private static CreateStaffCommand ValidCommand(string password = "Guclu.Sifre1") =>
        new("Ahmet", "Demir", "ahmet@campaigncell.com", password, nameof(Role.PERSONEL), "MARMARA",
            new[] { nameof(SegmentType.RISKLI_KAYIP) });

    [Fact]
    public void Gecerli_komut_hicbir_kurali_ihlal_etmez()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Kisa_sifre_min_uzunluk_kuralini_ihlal_eder()
    {
        var result = _validator.TestValidate(ValidCommand("Ab1!"));
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Sifre en az 8 karakter olmalidir.");
    }

    [Fact]
    public void Buyuk_harf_icermeyen_sifre_ilgili_kurali_ihlal_eder()
    {
        var result = _validator.TestValidate(ValidCommand("kucuk.harf1"));
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Sifre en az 1 buyuk harf icermelidir.");
    }

    [Fact]
    public void Rakam_icermeyen_sifre_ilgili_kurali_ihlal_eder()
    {
        var result = _validator.TestValidate(ValidCommand("Guclu.Sifre"));
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Sifre en az 1 rakam icermelidir.");
    }

    [Fact]
    public void Ozel_karakter_icermeyen_sifre_ilgili_kurali_ihlal_eder()
    {
        var result = _validator.TestValidate(ValidCommand("GucluSifre1"));
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Sifre en az 1 ozel karakter icermelidir.");
    }

    [Fact]
    public void Zayif_sifre_ihlal_edilen_TUM_kurallari_ayni_anda_dondurur()
    {
        // "ab" — kisa, buyuk harf yok, rakam yok, ozel karakter yok: 4 kural birden ihlal edilir.
        var result = _validator.TestValidate(ValidCommand("ab"));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Sifre en az 8 karakter olmalidir.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sifre en az 1 buyuk harf icermelidir.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sifre en az 1 rakam icermelidir.");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Sifre en az 1 ozel karakter icermelidir.");
    }

    [Fact]
    public void Musteri_rolu_ile_personel_olusturulamaz()
    {
        var command = ValidCommand() with { Role = nameof(Role.MUSTERI) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Bos_uzmanlik_listesi_reddedilir()
    {
        var command = ValidCommand() with { Expertise = Array.Empty<string>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Expertise);
    }
}
