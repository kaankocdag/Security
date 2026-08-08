using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class NotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public NotificationsController(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool onlyUnread = false, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Forbid();
        }

        var query = _db.Notifications
            .Where(n => n.UserId == userId || n.CompanyId == _currentUser.CompanyId);
        if (onlyUnread)
        {
            query = query.Where(n => !n.IsRead);
        }

        var list = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.IsRead,
                n.ReadAt,
                n.ActionUrl,
                n.Icon,
                n.RelatedEntityType,
                n.RelatedEntityId,
                n.CreatedAt
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (notification is null)
        {
            return NotFound();
        }
        notification.IsRead = true;
        notification.ReadAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
