using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IPassiveSecurityCheck
{
    string CheckCode { get; }
    string DisplayName { get; }
    string Category { get; }
    ScanType SupportedScanTypes { get; }
    int Order { get; }

    Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default);
}

public sealed class ScanContext
{
    public required Guid ScanJobId { get; init; }
    public required Guid CompanyId { get; init; }
    public required Uri TargetUri { get; init; }
    public required string NormalizedHostName { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public sealed record CheckOutcome(
    string CheckCode,
    CheckStatus Status,
    IReadOnlyList<CheckFinding> Findings,
    string? DiagnosticSummary = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record CheckFinding(
    string Title,
    string Description,
    Severity Severity,
    ConfidenceLevel Confidence,
    string Category,
    string? CweCode = null,
    string? OwaspCategory = null,
    string? AffectedUrl = null,
    string? AffectedParameter = null,
    string? Evidence = null,
    string? Remediation = null,
    string? RemediationExampleConfig = null,
    string? TurkishExecutiveSummary = null,
    string? BusinessImpact = null,
    string? TechnicalDescription = null,
    string? Fingerprint = null);

public enum CheckStatus
{
    Passed = 0,
    IssuesFound = 1,
    Skipped = 2,
    Failed = 3
}
