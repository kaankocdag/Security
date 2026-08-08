using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabAuditService : ILabAuditService
{
    private readonly IAuditLogWriter _audit;

    public LabAuditService(IAuditLogWriter audit)
    {
        _audit = audit;
    }

    public Task WriteAsync(
        Guid correlationId,
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            AuditCorrelationId = correlationId,
            Details = details
        };

        return _audit.WriteAsync(
            action,
            entityType,
            entityId,
            payload,
            category: LabConstants.AuditCategory,
            isSensitive: true,
            cancellationToken);
    }
}
