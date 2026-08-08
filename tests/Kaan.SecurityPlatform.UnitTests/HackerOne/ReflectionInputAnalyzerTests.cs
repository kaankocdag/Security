using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

public sealed class ReflectionInputAnalyzerTests
{
    [Fact]
    public void Properly_html_encoded_reflection_is_detected()
    {
        var marker = "kaanxssabc123\"'><";
        var body = "<div>hello kaanxssabc123&quot;&gt;&lt;</div>";
        using var response = new HttpResponseMessage();
        response.Content = new StringContent(body);
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

        var analysis = ReflectionInputAnalyzer.Analyze(
            marker, body, response.Headers, response.Content.Headers, 200);

        analysis.ProperlyEncoded.Should().BeTrue();
        analysis.HtmlEncoded.Should().BeTrue();
        analysis.ReflectionCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Html_text_context_is_inferred()
    {
        var marker = ReflectionInputAnalyzer.CreateHarmlessMarker();
        var token = marker.Split('"')[0];
        var body = $"<html><body><p>Search: {token}\"'&gt;</p></body></html>";
        using var response = new HttpResponseMessage { Content = new StringContent(body) };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");

        var analysis = ReflectionInputAnalyzer.Analyze(
            marker, body, response.Headers, response.Content.Headers, 200);

        analysis.Context.Should().Be(ReflectionContext.HtmlText);
        analysis.InputSource.Should().Be("query:q");
    }
}
