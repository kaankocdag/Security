using System.Security.Claims;
using Kaan.SecurityPlatform.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kaan.SecurityPlatform.Api.Hubs;

[Authorize]
public sealed class ActivityHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user is null)
        {
            await base.OnConnectedAsync();
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        var companyId = user.FindFirstValue(ClaimTypesExtended.CompanyId);
        if (!string.IsNullOrEmpty(companyId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"company:{companyId}");
        }

        if (user.IsInRole(Roles.SystemAdmin))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "role:system-admin");
        }

        await base.OnConnectedAsync();
    }
}
