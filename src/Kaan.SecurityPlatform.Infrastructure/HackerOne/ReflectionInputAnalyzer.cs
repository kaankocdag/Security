using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

/// <summary>
/// Harmless unique marker reflection analizi.
/// Executable JS payload veya aktif exploitation çalıştırmaz.
/// Encoding probe karakterleri yalnızca encode edilip edilmediğini anlamak içindir.
/// </summary>
public static class ReflectionInputAnalyzer
{
    /// <summary>Alphanumeric token + encoding canaries (script değil).</summary>
    public static string CreateHarmlessMarker()
    {
        var token = "kaanxss" + Guid.NewGuid().ToString("N")[..12];
        // Encoding detection only — not an executable payload
        return token + "\"'><";
    }

    public static ReflectionAnalysis Analyze(
        string marker,
        string responseBody,
        HttpResponseHeaders headers,
        HttpContentHeaders contentHeaders,
        int statusCode)
    {
        var contentType = contentHeaders.ContentType?.MediaType
                          ?? headers.GetValuesSafe("Content-Type").FirstOrDefault()
                          ?? "";
        var rawCount = CountOccurrences(responseBody, marker);
        var htmlEntityForms = BuildHtmlEntityForms(marker);
        var entityCount = htmlEntityForms.Sum(f => CountOccurrences(responseBody, f));
        var attrEncoded = DetectAttributeEncoding(responseBody, marker);
        var htmlEncoded = entityCount > 0 && rawCount == 0;

        // Properly encoded: special chars only appear encoded, or no raw special-char reflection
        var special = "\"'><";
        var specialRaw = special.Any(ch => responseBody.Contains(ch) && MarkerSpecialReflectedRaw(responseBody, marker, ch));
        var properlyEncoded = rawCount == 0
                              ? entityCount > 0 || attrEncoded
                              : !specialRaw && (entityCount > 0 || attrEncoded);

        // If alphanumeric token reflects but canaries are encoded → treat as encoded
        var tokenOnly = marker.Split('"')[0];
        var tokenRaw = CountOccurrences(responseBody, tokenOnly);
        if (tokenRaw > 0 && !ContainsUnencodedCanaries(responseBody, tokenOnly))
        {
            htmlEncoded = true;
            properlyEncoded = true;
            rawCount = tokenRaw;
        }

        var context = InferContext(responseBody, tokenOnly, contentType);
        var location = InferLocation(responseBody, tokenOnly);

        return new ReflectionAnalysis(
            Context: context,
            ReflectionCount: Math.Max(rawCount, entityCount),
            HtmlEncoded: htmlEncoded || properlyEncoded,
            AttributeEncoded: attrEncoded,
            ContentType: contentType,
            HttpStatus: statusCode,
            ReflectionLocation: location,
            InputSource: "query:q",
            Marker: marker,
            ProperlyEncoded: properlyEncoded,
            RawUnencodedSpecialChars: specialRaw && tokenRaw > 0);
    }

    private static bool ContainsUnencodedCanaries(string body, string token)
    {
        var idx = 0;
        while ((idx = body.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
        {
            var after = idx + token.Length;
            if (after < body.Length)
            {
                var slice = body.Substring(after, Math.Min(8, body.Length - after));
                if (slice.Contains('"') || slice.Contains('\'') || slice.Contains('<') || slice.Contains('>'))
                {
                    // Not entity-encoded immediately after token
                    if (!slice.StartsWith("&quot;", StringComparison.OrdinalIgnoreCase)
                        && !slice.StartsWith("&#", StringComparison.Ordinal)
                        && !slice.StartsWith("&lt;", StringComparison.OrdinalIgnoreCase)
                        && !slice.StartsWith("&gt;", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            idx += token.Length;
        }

        return false;
    }

    private static bool MarkerSpecialReflectedRaw(string body, string marker, char ch)
    {
        var token = marker.Split(ch)[0];
        var idx = body.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0)
        {
            return false;
        }

        var window = body.Substring(idx, Math.Min(token.Length + 6, body.Length - idx));
        return window.Contains(ch);
    }

    private static IReadOnlyList<string> BuildHtmlEntityForms(string marker)
    {
        var forms = new List<string>();
        var encoded = new StringBuilder();
        foreach (var ch in marker)
        {
            encoded.Append(ch switch
            {
                '"' => "&quot;",
                '\'' => "&#39;",
                '<' => "&lt;",
                '>' => "&gt;",
                '&' => "&amp;",
                _ => ch.ToString()
            });
        }

        forms.Add(encoded.ToString());
        forms.Add(marker.Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal));
        return forms.Distinct().ToList();
    }

    private static bool DetectAttributeEncoding(string body, string marker)
    {
        var token = marker.Split('"')[0];
        // attribute="...token..." or attribute='...'
        var patterns = new[]
        {
            $@"=\s*""[^""]*{Regex.Escape(token)}[^""]*""",
            $@"=\s*'[^']*{Regex.Escape(token)}[^']*'"
        };
        foreach (var p in patterns)
        {
            if (Regex.IsMatch(body, p, RegexOptions.IgnoreCase))
            {
                // If quotes from marker are not breaking out of attribute, attribute-encoded
                var breakOut = Regex.IsMatch(body, $@"=\s*""[^""]*{Regex.Escape(token)}""", RegexOptions.IgnoreCase)
                               && body.Contains(token + "\"", StringComparison.Ordinal);
                return !breakOut || body.Contains(token + "&quot;", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    private static ReflectionContext InferContext(string body, string token, string contentType)
    {
        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return ReflectionContext.Json;
        }

        var idx = body.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0)
        {
            return ReflectionContext.Unknown;
        }

        var before = body[..idx];
        var afterStart = Math.Min(body.Length, idx + token.Length);
        var after = body[afterStart..Math.Min(body.Length, afterStart + 80)];

        if (Regex.IsMatch(before, @"<script[^>]*>[\s\S]*$", RegexOptions.IgnoreCase)
            && !before.Contains("</script>", StringComparison.OrdinalIgnoreCase))
        {
            return ReflectionContext.Script;
        }

        if (Regex.IsMatch(before, @"=\s*[""'][^""']*$"))
        {
            return ReflectionContext.HtmlAttribute;
        }

        if (after.Contains("://") || before.Contains("href=", StringComparison.OrdinalIgnoreCase)
                                  || before.Contains("src=", StringComparison.OrdinalIgnoreCase))
        {
            return ReflectionContext.Url;
        }

        if (before.Contains('<') || after.Contains('<') || after.Contains('>'))
        {
            return ReflectionContext.HtmlText;
        }

        return ReflectionContext.Unknown;
    }

    private static string InferLocation(string body, string token)
    {
        var idx = body.IndexOf(token, StringComparison.Ordinal);
        if (idx < 0)
        {
            return "none";
        }

        var start = Math.Max(0, idx - 40);
        var len = Math.Min(100, body.Length - start);
        var snippet = body.Substring(start, len).Replace('\n', ' ').Replace('\r', ' ');
        return snippet.Length <= 100 ? snippet : snippet[..100];
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle))
        {
            return 0;
        }

        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }

    private static IEnumerable<string> GetValuesSafe(this HttpResponseHeaders headers, string name)
    {
        return headers.TryGetValues(name, out var values) ? values : Array.Empty<string>();
    }
}

public sealed record ReflectionAnalysis(
    ReflectionContext Context,
    int ReflectionCount,
    bool HtmlEncoded,
    bool AttributeEncoded,
    string ContentType,
    int HttpStatus,
    string ReflectionLocation,
    string InputSource,
    string Marker,
    bool ProperlyEncoded,
    bool RawUnencodedSpecialChars);
