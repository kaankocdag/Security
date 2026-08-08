using Kaan.SecurityPlatform.Application.Common.Exceptions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Common;

/// <summary>
/// SystemAdmin firmasız JWT ile geldiğinde platform/demo firmasına düşer.
/// </summary>
public static class CompanyIdResolver
{
    public static async Task<Guid> ResolveAsync(
        ICurrentUser currentUser,
        IApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is Guid companyId)
        {
            return companyId;
        }

        if (!currentUser.IsSystemAdmin)
        {
            throw new ForbiddenAccessException("Bu işlem için bir firmaya bağlı olmalısınız.");
        }

        var fallback = await db.Companies
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (fallback is null)
        {
            throw new ForbiddenAccessException(
                "SystemAdmin için kullanılacak firma bulunamadı. API'yi yeniden başlatın (seed) veya bir firma onaylayın.");
        }

        return fallback.Value;
    }
}
