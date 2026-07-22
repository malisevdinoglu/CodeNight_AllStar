using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Xunit;

namespace Identity.UnitTests.Domain;

/// <summary>
/// Core_Principles §10: 5 hatalı girişte 15 dk kilit. Kilitleme mantığı domain'de yaşar
/// (anemik model değil) — Application katmanı sadece bu metotları çağırır.
/// </summary>
public sealed class UserLockoutTests
{
    private static readonly DateTime Now = new(2026, 7, 22, 12, 0, 0, DateTimeKind.Utc);

    private static User CreateStaffUser() =>
        User.CreateStaff("Zeynep", "Kaya", "zeynep@campaigncell.com", "hashed", Role.PERSONEL, "EGE", Array.Empty<SegmentType>());

    [Fact]
    public void Dorduncu_basarisiz_denemede_hesap_kilitlenmez()
    {
        var user = CreateStaffUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts - 1; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.FailedLoginCount.Should().Be(User.MaxFailedLoginAttempts - 1);
        user.IsCurrentlyLocked(Now).Should().BeFalse();
    }

    [Fact]
    public void Besinci_basarisiz_denemede_hesap_15_dakika_kilitlenir()
    {
        var user = CreateStaffUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.IsCurrentlyLocked(Now).Should().BeTrue();
        user.RemainingLockSeconds(Now).Should().Be(User.LockDurationMinutes * 60);
    }

    [Fact]
    public void Kilit_suresi_dolunca_hesap_tekrar_acilir()
    {
        var user = CreateStaffUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        var afterLock = Now.AddMinutes(User.LockDurationMinutes).AddSeconds(1);

        user.IsCurrentlyLocked(afterLock).Should().BeFalse();
        user.RemainingLockSeconds(afterLock).Should().Be(0);
    }

    [Fact]
    public void Basarili_giris_sayaci_ve_kilidi_sifirlar()
    {
        var user = CreateStaffUser();

        for (var i = 0; i < User.MaxFailedLoginAttempts; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.IsCurrentlyLocked(Now).Should().BeTrue();

        user.ResetFailedLoginCount();

        user.FailedLoginCount.Should().Be(0);
        user.IsCurrentlyLocked(Now).Should().BeFalse();
    }
}
