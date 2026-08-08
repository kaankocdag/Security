using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class RootCauseGroupService : IRootCauseGroupService
{
    private readonly IApplicationDbContext _db;

    public RootCauseGroupService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> AssignAsync(
        Guid findingId,
        string? fingerprint,
        string title,
        CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrWhiteSpace(fingerprint)
            ? $"finding:{findingId:N}"
            : fingerprint.Trim().ToLowerInvariant();

        var group = await _db.RootCauseGroups.FirstOrDefaultAsync(g => g.FingerprintKey == key, cancellationToken);
        if (group is null)
        {
            group = new RootCauseGroup
            {
                FingerprintKey = key,
                Title = title.Length > 256 ? title[..256] : title,
                FindingCount = 0
            };
            _db.RootCauseGroups.Add(group);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var finding = await _db.Findings.FirstOrDefaultAsync(f => f.Id == findingId, cancellationToken);
        if (finding is null)
        {
            return group.Id;
        }

        if (finding.RootCauseGroupId == group.Id)
        {
            return group.Id;
        }

        finding.RootCauseGroupId = group.Id;
        await _db.SaveChangesAsync(cancellationToken);
        group.FindingCount = await _db.Findings.CountAsync(f => f.RootCauseGroupId == group.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}
