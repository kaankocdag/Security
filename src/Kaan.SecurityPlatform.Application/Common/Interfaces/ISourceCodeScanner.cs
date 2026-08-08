namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface ISourceCodeScanner
{
    string Provider { get; }
    Task<IReadOnlyList<CheckFinding>> ScanAsync(SourceScanRequest request, CancellationToken cancellationToken = default);
}

public interface IDependencyScanner
{
    string Provider { get; }
    Task<IReadOnlyList<CheckFinding>> ScanAsync(SourceScanRequest request, CancellationToken cancellationToken = default);
}

public interface ISecretScanner
{
    string Provider { get; }
    Task<IReadOnlyList<CheckFinding>> ScanAsync(SourceScanRequest request, CancellationToken cancellationToken = default);
}

public sealed record SourceScanRequest(
    Guid ScanJobId,
    Guid CompanyId,
    string RepositoryUrl,
    string? Branch = null,
    string? AccessToken = null);
