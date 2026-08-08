using Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.AuthenticatedScanning;

public class LoginPageDiscoveryServiceTests
{
    private readonly LoginPageDiscoveryService _sut = new();

    [Fact]
    public void Ayni_site_login_linkleri_bulunur()
    {
        const string html = """
            <html><body>
              <a href="/hesap/giris">Giriş Yap</a>
              <a href="https://app.example.com/auth/login">Sign in</a>
              <a href="/blog">Blog</a>
            </body></html>
            """;

        var links = _sut.ExtractLoginLinks(html, "https://example.com/");

        Assert.Contains("https://example.com/hesap/giris", links);
        Assert.Contains("https://app.example.com/auth/login", links);
        Assert.DoesNotContain("https://example.com/blog", links);
    }

    [Fact]
    public void Ucuncu_taraf_login_linkleri_onerilmez()
    {
        const string html = """
            <html><body><a href="https://accounts.google.com/signin">Google ile giriş</a></body></html>
            """;

        var links = _sut.ExtractLoginLinks(html, "https://example.com/");

        Assert.Empty(links);
    }

    [Fact]
    public void OAuth_saglayicilari_tespit_edilir()
    {
        const string html = """
            <html><body>
              <button data-provider="google">Continue with Google</button>
              <a href="https://login.microsoftonline.com/x">Microsoft ile</a>
            </body></html>
            """;

        var providers = _sut.DetectOAuthProviders(html);

        Assert.Contains("Google", providers);
        Assert.Contains("Microsoft", providers);
    }

    [Theory]
    [InlineData("<input type=\"password\" name=\"pw\" />", true)]
    [InlineData("<input type=\"email\" name=\"mail\" />", false)]
    public void Sifre_formu_dogru_tespit_edilir(string body, bool expected) =>
        Assert.Equal(expected, _sut.HasPasswordForm($"<html><body><form>{body}</form></body></html>"));
}
