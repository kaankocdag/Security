namespace Kaan.SecurityPlatform.Domain.Enums;

/// <summary>
/// Bulgunun güvenlik değerlendirmesindeki sınıfı.
/// Scanner çıktısı otomatik olarak Vulnerability sayılmaz.
/// </summary>
public enum FindingClass
{
    Vulnerability = 0,
    SecurityMisconfiguration = 1,
    HardeningRecommendation = 2,
    Informational = 3,
    ComplianceIssue = 4,
    SeoIssue = 5,
    /// <summary>Exploit / browser-side impact doğrulanmamış aday bulgu.</summary>
    VulnerabilityCandidate = 6
}

/// <summary>HackerOne / BB rapor şiddeti — doğrulanmamışsa Unassigned.</summary>
public enum BugBountySeverity
{
    Unassigned = 0,
    None = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

/// <summary>Yansıyan girdinin bulunduğu yanıt bağlamı.</summary>
public enum ReflectionContext
{
    Unknown = 0,
    HtmlText = 1,
    HtmlAttribute = 2,
    Script = 3,
    Json = 4,
    Url = 5,
    Header = 6
}

/// <summary>
/// Bug bounty programına gönderim önerisi.
/// </summary>
public enum SubmissionRecommendation
{
    DoNotSubmit = 0,
    ManualReview = 1,
    Submit = 2
}

/// <summary>
/// Teknik istismar edilebilirlik seviyesi (scanner severity'den bağımsız).
/// </summary>
public enum Exploitability
{
    None = 0,
    Theoretical = 1,
    RequiresPreconditions = 2,
    Practical = 3,
    Demonstrated = 4
}

/// <summary>
/// Program politikası kategori anahtarları (Amazon VRP vb.).
/// </summary>
public enum BugBountyPolicyCategory
{
    ScannerOutputOnly = 0,
    MissingSecurityHeaders = 1,
    MissingCookieFlags = 2,
    Clickjacking = 3,
    InformationDisclosure = 4,
    MisconfigurationWithDemonstratedImpact = 5,
    Xss = 10,
    SqlInjection = 11,
    Idor = 12,
    AuthenticationBypass = 13,
    PrivilegeEscalation = 14,
    Other = 99
}
