using Kaan.SecurityPlatform.Application.Features.Reports;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Reporting;

internal sealed class ReportCopy
{
    public required string DocumentTitle { get; init; }
    public required string VendorSubtitle { get; init; }
    public required string CoverHeading { get; init; }
    public required string TargetDomain { get; init; }
    public required string ScanJobIdLabel { get; init; }
    public required string ReportDate { get; init; }
    public required string SecurityScore { get; init; }
    public required string Checks { get; init; }
    public required string FindingSummary { get; init; }
    public required string Passed { get; init; }
    public required string Issues { get; init; }
    public required string Skipped { get; init; }
    public required string Total { get; init; }
    public required string Critical { get; init; }
    public required string High { get; init; }
    public required string Medium { get; init; }
    public required string Low { get; init; }
    public required string Info { get; init; }
    public required string Intro { get; init; }
    public required string ExecutiveHeading { get; init; }
    public required string TechnicalHeading { get; init; }
    public required string FindingsHeading { get; init; }
    public required string NoFindings { get; init; }
    public required string FindingLabel { get; init; }
    public required string Title { get; init; }
    public required string Severity { get; init; }
    public required string Confidence { get; init; }
    public required string Category { get; init; }
    public required string AffectedUrl { get; init; }
    public required string Parameter { get; init; }
    public required string CheckCode { get; init; }
    public required string Fingerprint { get; init; }
    public required string Status { get; init; }
    public required string Description { get; init; }
    public required string FindingExecSummary { get; init; }
    public required string TechnicalDesc { get; init; }
    public required string BusinessImpact { get; init; }
    public required string Evidence { get; init; }
    public required string Remediation { get; init; }
    public required string ExampleConfig { get; init; }
    public required string VendorSnippetHeading { get; init; }
    public required string ClosingHeading { get; init; }
    public required string ClosingBody { get; init; }
    public required string GeneratedBy { get; init; }
    public required string ContentNote { get; init; }
    public required string CategoryCol { get; init; }
    public required string CountCol { get; init; }
    public required string ScoreLabel { get; init; }
    public required string SummaryHeading { get; init; }
    public required string FindingsHtmlHeading { get; init; }
    public required string RemediationHtmlHeading { get; init; }
    public required string LanguageTag { get; init; }
    public required string NoSummary { get; init; }
    public required string AssessmentSection { get; init; }
    public required string BugBountySection { get; init; }
    public required string BugBountyEmpty { get; init; }
    public required string ScannerSeverity { get; init; }
    public required string TechnicalSeverityLabel { get; init; }
    public required string FindingClassLabel { get; init; }
    public required string BbEligibleLabel { get; init; }
    public required string SubmissionLabel { get; init; }

    public static ReportCopy For(ReportLanguage language) =>
        language == ReportLanguage.En ? En() : Tr();

    public string SeverityLabel(Severity s) => LanguageTag == "en"
        ? s switch
        {
            Domain.Enums.Severity.Critical => "Critical",
            Domain.Enums.Severity.High => "High",
            Domain.Enums.Severity.Medium => "Medium",
            Domain.Enums.Severity.Low => "Low",
            _ => "Informational"
        }
        : s switch
        {
            Domain.Enums.Severity.Critical => "Kritik",
            Domain.Enums.Severity.High => "Yüksek",
            Domain.Enums.Severity.Medium => "Orta",
            Domain.Enums.Severity.Low => "Düşük",
            _ => "Bilgilendirme"
        };

    public string VendorRequest(string host, string title, string severity, string? cwe, string? url, string? remediation)
    {
        if (LanguageTag == "en")
        {
            return
                $"Hello, our security assessment of {host} identified \"{title}\" " +
                $"(severity: {severity}, CWE: {cwe ?? "N/A"}). " +
                $"Affected resource: {url ?? host}. " +
                $"Please apply the following remediation: {remediation ?? "Review the related security header / configuration."} " +
                "Please notify us after the change so we can retest.";
        }

        return
            $"Merhaba, {host} için güvenlik değerlendirmemizde \"{title}\" bulgusu tespit edildi " +
            $"(şiddet: {severity}, CWE: {cwe ?? "N/A"}). " +
            $"Etkilenen kaynak: {url ?? host}. " +
            $"Lütfen şu düzeltmeyi uygulayın: {remediation ?? "İlgili güvenlik başlığı / yapılandırmayı gözden geçirin."} " +
            "Uygulama sonrası bilgilendirirseniz yeniden test edeceğiz.";
    }

    private static ReportCopy Tr() => new()
    {
        LanguageTag = "tr",
        DocumentTitle = "Kaan Güvenlik Raporu",
        VendorSubtitle = "Firma / satıcı destek ekiplerine iletilebilir düz metin formatı",
        CoverHeading = "1. KAPAK / İLETİŞİM ÖZETİ",
        TargetDomain = "Hedef domain",
        ScanJobIdLabel = "Tarama iş kimliği",
        ReportDate = "Rapor tarihi (UTC)",
        SecurityScore = "Güvenlik puanı",
        Checks = "Kontroller",
        FindingSummary = "Bulgu özeti",
        Passed = "geçti",
        Issues = "sorun",
        Skipped = "atlandı",
        Total = "toplam",
        Critical = "Kritik",
        High = "Yüksek",
        Medium = "Orta",
        Low = "Düşük",
        Info = "Bilgi",
        Intro =
            "Bu rapor, kamuya açık veya yetkilendirilmiş pasif güvenlik kontrollerinin sonuçlarını içerir. " +
            "Exploit / payload çalıştırılmaz. Lütfen aşağıdaki bulguları güvenlik veya altyapı ekibinize iletin " +
            "(ör. Amazon CloudFront / ALB / WAF, hosting paneli, CDN, reverse proxy yapılandırması).",
        ExecutiveHeading = "2. YÖNETİCİ ÖZETİ",
        TechnicalHeading = "3. TEKNİK ÖZET",
        FindingsHeading = "4. DETAYLI BULGULAR (şiddete göre)",
        NoFindings = "Bu taramada açık bulgu üretilmedi.",
        FindingLabel = "Bulgu",
        Title = "Başlık",
        Severity = "Şiddet",
        Confidence = "Güven seviyesi",
        Category = "Kategori",
        AffectedUrl = "Etkilenen URL",
        Parameter = "Parametre",
        CheckCode = "Kontrol kodu",
        Fingerprint = "Parmak izi",
        Status = "Durum",
        Description = "Açıklama",
        FindingExecSummary = "Yönetici özeti (bulgu)",
        TechnicalDesc = "Teknik açıklama",
        BusinessImpact = "İş etkisi",
        Evidence = "Kanıt",
        Remediation = "Önerilen çözüm (firmaya iletilebilir)",
        ExampleConfig = "Örnek yapılandırma",
        VendorSnippetHeading = "Satıcıya örnek talep metni",
        ClosingHeading = "5. KAPANIŞ",
        ClosingBody = "Düzeltme sonrası platformda yeniden tarama / yeniden test ile doğrulama yapılabilir.",
        GeneratedBy = "Rapor Kaan Security Platform tarafından üretilmiştir.",
        ContentNote = "Bulgu metinleri tarayıcı çıktısıdır (çoğunlukla Türkçe).",
        CategoryCol = "Kategori",
        CountCol = "Adet",
        ScoreLabel = "Puan",
        SummaryHeading = "Özet",
        FindingsHtmlHeading = "Bulgular",
        RemediationHtmlHeading = "Düzeltme önerisi",
        NoSummary = "Özet bulunamadı.",
        AssessmentSection = "1. Security Assessment Findings (Güvenlik Değerlendirme Bulguları)",
        BugBountySection = "2. Bug Bounty Submission Candidates",
        BugBountyEmpty =
            "Bu taramada Amazon VRP / HackerOne için gönderilebilir aday yok. " +
            "Eksik header / hardening bulguları tek başına vulnerability sayılmaz.",
        ScannerSeverity = "Scanner şiddeti",
        TechnicalSeverityLabel = "Teknik şiddet",
        FindingClassLabel = "Sınıf",
        BbEligibleLabel = "BB uygun",
        SubmissionLabel = "Gönderim önerisi"
    };

    private static ReportCopy En() => new()
    {
        LanguageTag = "en",
        DocumentTitle = "Kaan Security Report",
        VendorSubtitle = "Plain-text format suitable for vendor / support tickets (e.g. Amazon, CDN, hosting)",
        CoverHeading = "1. COVER / CONTACT SUMMARY",
        TargetDomain = "Target domain",
        ScanJobIdLabel = "Scan job ID",
        ReportDate = "Report date (UTC)",
        SecurityScore = "Security score",
        Checks = "Checks",
        FindingSummary = "Finding summary",
        Passed = "passed",
        Issues = "issues",
        Skipped = "skipped",
        Total = "total",
        Critical = "Critical",
        High = "High",
        Medium = "Medium",
        Low = "Low",
        Info = "Info",
        Intro =
            "This report contains results of public or authorized passive security checks. " +
            "No exploit or destructive payload is executed. Please forward the findings below to your " +
            "security or infrastructure team (e.g. Amazon CloudFront / ALB / WAF, hosting panel, CDN, reverse proxy).",
        ExecutiveHeading = "2. EXECUTIVE SUMMARY",
        TechnicalHeading = "3. TECHNICAL SUMMARY",
        FindingsHeading = "4. DETAILED FINDINGS (by severity)",
        NoFindings = "No open findings were produced for this scan.",
        FindingLabel = "Finding",
        Title = "Title",
        Severity = "Severity",
        Confidence = "Confidence",
        Category = "Category",
        AffectedUrl = "Affected URL",
        Parameter = "Parameter",
        CheckCode = "Check code",
        Fingerprint = "Fingerprint",
        Status = "Status",
        Description = "Description",
        FindingExecSummary = "Executive summary (finding)",
        TechnicalDesc = "Technical description",
        BusinessImpact = "Business impact",
        Evidence = "Evidence",
        Remediation = "Recommended remediation (vendor-ready)",
        ExampleConfig = "Example configuration",
        VendorSnippetHeading = "Sample vendor request text",
        ClosingHeading = "5. CLOSING",
        ClosingBody = "After remediation, re-scan / retest in the platform to verify the fix.",
        GeneratedBy = "Generated by Kaan Security Platform.",
        ContentNote = "Finding body text is scanner output (often Turkish). Labels and vendor templates are in English.",
        CategoryCol = "Category",
        CountCol = "Count",
        ScoreLabel = "Score",
        SummaryHeading = "Summary",
        FindingsHtmlHeading = "Findings",
        RemediationHtmlHeading = "Remediation",
        NoSummary = "No summary available.",
        AssessmentSection = "1. Security Assessment Findings",
        BugBountySection = "2. Bug Bounty Submission Candidates",
        BugBountyEmpty =
            "No Amazon VRP / HackerOne submission candidates in this scan. " +
            "Missing headers / hardening items alone are not treated as vulnerabilities.",
        ScannerSeverity = "Scanner severity",
        TechnicalSeverityLabel = "Technical severity",
        FindingClassLabel = "Class",
        BbEligibleLabel = "BB eligible",
        SubmissionLabel = "Submission recommendation"
    };
}
