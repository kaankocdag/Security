namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification;

public sealed class DomainVerificationOptions
{
    public const string SectionName = "DomainVerification";

    public string TxtRecordPrefix { get; set; } = "_kaan-security";
    public string HtmlFilePath { get; set; } = "/.well-known/kaan-security-verification.txt";
    public string MetaTagName { get; set; } = "kaan-security-verification";
    public bool EnableMockStrategy { get; set; }
    public string MockAutoApproveToken { get; set; } = "kaan-dev-approve";
    public int TimeoutSeconds { get; set; } = 8;
}
