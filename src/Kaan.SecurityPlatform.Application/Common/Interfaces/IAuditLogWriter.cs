namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        string? category = null,
        bool isSensitive = false,
        CancellationToken cancellationToken = default);
}
