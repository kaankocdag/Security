using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.HackerOne;

/// <summary>
/// HackerOne export language is fixed (en-US) and independent from UI language.
/// </summary>
public static class HackerOneReportLanguage
{
    public const string Code = "en-US";

    public static string FormatSubmissionRecommendation(SubmissionRecommendation value) => value switch
    {
        SubmissionRecommendation.DoNotSubmit => "Do Not Submit",
        SubmissionRecommendation.ManualReview => "Manual Review",
        SubmissionRecommendation.Submit => "Submit",
        _ => value.ToString()
    };

    public static string FormatExploitability(Exploitability value, bool requiresManualValidation) =>
        requiresManualValidation || value is Exploitability.None or Exploitability.Theoretical
            ? "Requires Manual Validation"
            : value switch
            {
                Exploitability.RequiresPreconditions => "Requires Preconditions",
                Exploitability.Practical => "Practical",
                Exploitability.Demonstrated => "Demonstrated",
                _ => value.ToString()
            };

    public static string FormatFindingType(
        string? fingerprint,
        FindingClass findingClass,
        BugBountyPolicyCategory policyCategory)
    {
        if (string.Equals(fingerprint, "asc.xss.reflected-marker", StringComparison.OrdinalIgnoreCase)
            || policyCategory == BugBountyPolicyCategory.Xss)
        {
            return "XSS Candidate";
        }

        if (fingerprint is not null
            && fingerprint.Equals("asc.access.confirmed-unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return "Broken Access Control";
        }

        if (fingerprint is not null
            && fingerprint.Equals("asc.access.surface-donotsubmit", StringComparison.OrdinalIgnoreCase))
        {
            return "Informational";
        }

        if (fingerprint is not null
            && fingerprint.StartsWith("asc.access.", StringComparison.OrdinalIgnoreCase))
        {
            return "AccessControlCandidate";
        }

        if (policyCategory == BugBountyPolicyCategory.PrivilegeEscalation
            && findingClass == FindingClass.VulnerabilityCandidate)
        {
            return "AccessControlCandidate";
        }

        return findingClass == FindingClass.VulnerabilityCandidate
            ? "Vulnerability Candidate"
            : findingClass.ToString();
    }

    public static bool IsXssCandidate(string? fingerprint, BugBountyPolicyCategory policyCategory) =>
        string.Equals(fingerprint, "asc.xss.reflected-marker", StringComparison.OrdinalIgnoreCase)
        || policyCategory == BugBountyPolicyCategory.Xss;

    public static bool IsAccessControlCandidate(string? fingerprint, BugBountyPolicyCategory policyCategory) =>
        (fingerprint is not null && fingerprint.StartsWith("asc.access.", StringComparison.OrdinalIgnoreCase))
        || (policyCategory == BugBountyPolicyCategory.PrivilegeEscalation
            && !IsXssCandidate(fingerprint, policyCategory));

    public static string FormatCandidateSeverity(Severity? technicalPotential, BugBountySeverity bbSeverity)
    {
        if (bbSeverity != BugBountySeverity.Unassigned)
        {
            return bbSeverity.ToString();
        }

        return technicalPotential?.ToString() ?? "Medium";
    }
}
