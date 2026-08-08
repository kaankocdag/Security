namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

/// <summary>
/// Bir tarama işini uçtan uca çalıştıran orkestratör.
/// ScannerWorker Hangfire kuyruğundan bu servisi çağırır.
/// </summary>
public interface IScanExecutor
{
    Task ExecuteAsync(Guid scanJobId, CancellationToken cancellationToken = default);
}
