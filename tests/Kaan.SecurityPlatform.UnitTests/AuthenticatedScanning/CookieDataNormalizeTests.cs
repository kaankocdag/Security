using Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.AuthenticatedScanning;

public class CookieDataNormalizeTests
{
    [Fact]
    public void Ham_baslik_oldugu_gibi_normalize_edilir()
    {
        var result = AuthenticatedScanOrchestrator.NormalizeCookieData("sessionid=abc; csrftoken=xyz");

        Assert.Equal("sessionid=abc; csrftoken=xyz", result);
    }

    [Fact]
    public void Cookie_baslik_oneki_temizlenir()
    {
        var result = AuthenticatedScanOrchestrator.NormalizeCookieData("Cookie: a=1; b=2");

        Assert.Equal("a=1; b=2", result);
    }

    [Fact]
    public void Json_dizisi_baslik_stringine_cevrilir()
    {
        const string json = """
            [{"name":"sessionid","value":"abc","domain":".example.com"},
             {"name":"csrftoken","value":"xyz"}]
            """;

        var result = AuthenticatedScanOrchestrator.NormalizeCookieData(json);

        Assert.Equal("sessionid=abc; csrftoken=xyz", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("gecersiz veri")]
    [InlineData("[]")]
    public void Gecersiz_veri_null_doner(string input) =>
        Assert.Null(AuthenticatedScanOrchestrator.NormalizeCookieData(input));
}
