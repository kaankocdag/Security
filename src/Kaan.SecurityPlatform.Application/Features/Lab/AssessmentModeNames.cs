namespace Kaan.SecurityPlatform.Application.Features.Lab;

/// <summary>
/// Desteklenen / yasaklı değerlendirme modu adları (API/dokümantasyon).
/// </summary>
public static class AssessmentModeNames
{
    public const string PublicPassiveAssessment = "PublicPassiveAssessment";
    public const string IsolatedSecurityLab = "IsolatedSecurityLab";
    public const string AuthorizedExternalAssessment = "AuthorizedExternalAssessment";
    public const string ApplicationSecurityCandidate = "ApplicationSecurityCandidate";

    public static readonly string[] Supported =
    [
        PublicPassiveAssessment,
        IsolatedSecurityLab,
        AuthorizedExternalAssessment,
        ApplicationSecurityCandidate
    ];

    /// <summary>Eski / alternatif adlar — desteklenmez.</summary>
    public static readonly string[] ForbiddenForever =
    [
        "ActiveExternalAssessment",
        "ExternalActiveScan",
        "AuthorizedActiveTest"
    ];
}
