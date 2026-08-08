using System.Text.Json;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Entities.Audit;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditLogWriter(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        string? category = null,
        bool isSensitive = false,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.Email,
            CompanyId = _currentUser.CompanyId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            Category = category,
            IsSensitive = isSensitive,
            Details = details is null ? null : JsonSerializer.Serialize(details),
            CreatedAt = _clock.UtcNow
        };

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
