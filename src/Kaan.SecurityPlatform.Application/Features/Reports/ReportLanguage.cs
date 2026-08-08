namespace Kaan.SecurityPlatform.Application.Features.Reports;

public enum ReportLanguage
{
    Tr = 0,
    En = 1
}

public static class ReportLanguageParser
{
    public static ReportLanguage Parse(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return ReportLanguage.Tr;
        }

        return lang.Trim().ToLowerInvariant() switch
        {
            "en" or "en-us" or "en-gb" or "english" => ReportLanguage.En,
            _ => ReportLanguage.Tr
        };
    }

    public static string ToCode(ReportLanguage lang) => lang == ReportLanguage.En ? "en" : "tr";
}
