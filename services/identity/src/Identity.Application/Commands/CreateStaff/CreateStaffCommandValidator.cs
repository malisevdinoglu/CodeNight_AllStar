using FluentValidation;
using Identity.Domain.Enums;

namespace Identity.Application.Commands.CreateStaff;

/// <summary>
/// Şifre politikası (case §3.1): min 8 karakter, en az 1 büyük harf, 1 rakam, 1 özel karakter —
/// HER kural AYRI bir RuleFor bloğunda, ihlal edilen her kural kendi mesajıyla döner
/// (Core_Principles §10: "hangi kuralin ihlal edildigi mesajda").
/// </summary>
public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(60).WithMessage("Ad zorunludur.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(60).WithMessage("Soyad zorunludur.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("E-posta formati gecersiz.")
            .MaximumLength(120);

        RuleFor(x => x.Password).NotEmpty().WithMessage("Sifre zorunludur.");
        RuleFor(x => x.Password)
            .Must(p => (p ?? string.Empty).Length >= 8)
            .WithMessage("Sifre en az 8 karakter olmalidir.");
        RuleFor(x => x.Password)
            .Must(p => (p ?? string.Empty).Any(char.IsUpper))
            .WithMessage("Sifre en az 1 buyuk harf icermelidir.");
        RuleFor(x => x.Password)
            .Must(p => (p ?? string.Empty).Any(char.IsDigit))
            .WithMessage("Sifre en az 1 rakam icermelidir.");
        RuleFor(x => x.Password)
            .Must(p => (p ?? string.Empty).Any(c => !char.IsLetterOrDigit(c)))
            .WithMessage("Sifre en az 1 ozel karakter icermelidir.");

        RuleFor(x => x.Role)
            .Must(r => r is nameof(Role.PERSONEL) or nameof(Role.SUPERVIZOR))
            .WithMessage("Rol PERSONEL veya SUPERVIZOR olmalidir.");

        RuleFor(x => x.Region).NotEmpty().WithMessage("Bolge zorunludur.");

        RuleFor(x => x.Expertise)
            .NotEmpty().WithMessage("En az 1 uzmanlik alani secilmelidir.");

        RuleForEach(x => x.Expertise)
            .Must(e => Enum.TryParse<SegmentType>(e, out _))
            .WithMessage("Gecersiz uzmanlik alani.");
    }
}
