using System.Text.Json;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class BugBountyAuditWriter : IBugBountyAuditWriter
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public BugBountyAuditWriter(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        _db.BugBountyAuditLogs.Add(new BugBountyAuditLog
        {
            ActorUserId = _currentUser.UserId,
            ActorEmail = _currentUser.Email,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
