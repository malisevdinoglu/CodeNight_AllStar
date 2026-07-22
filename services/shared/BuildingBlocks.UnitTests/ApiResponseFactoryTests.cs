using BuildingBlocks.Common;
using FluentAssertions;
using Xunit;

namespace BuildingBlocks.UnitTests;

public class ApiResponseFactoryTests
{
    [Fact]
    public void Success_basari_true_ve_veriyi_tasimali()
    {
        var response = ApiResponseFactory.Success(new { id = 1 });

        response.Success.Should().BeTrue();
        response.Error.Should().BeNull();
        response.Data.Should().NotBeNull();
    }

    [Fact]
    public void Failure_basari_false_ve_hata_zarfini_tasimali()
    {
        var response = ApiResponseFactory.Failure<object?>(
            "CMP_422_INVALID_TRANSITION",
            "Gecersiz durum gecisi.",
            new[] { "Detay 1" });

        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be("CMP_422_INVALID_TRANSITION");
        response.Error.Details.Should().ContainSingle().Which.Should().Be("Detay 1");
    }

    [Fact]
    public void Failure_details_verilmezse_bos_liste_olmali()
    {
        var response = ApiResponseFactory.Failure<object?>("AUTH_400_VALIDATION", "Dogrulama hatasi.");

        response.Error!.Details.Should().BeEmpty();
    }
}
