using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Kaan.SecurityPlatform.Api.Hubs;

public sealed class SignalRActivityEventPublisher : IActivityEventPublisher
{
    private readonly IHubContext<ActivityHub> _hub;

    public SignalRActivityEventPublisher(IHubContext<ActivityHub> hub)
    {
        _hub = hub;
    }

    public Task PublishToUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default)
        => _hub.Clients.Group($"user:{userId}").SendAsync(eventType, payload, cancellationToken);

    public Task PublishToCompanyAsync(Guid companyId, string eventType, object payload, CancellationToken cancellationToken = default)
        => _hub.Clients.Group($"company:{companyId}").SendAsync(eventType, payload, cancellationToken);

    public Task PublishToSystemAdminsAsync(string eventType, object payload, CancellationToken cancellationToken = default)
        => _hub.Clients.Group("role:system-admin").SendAsync(eventType, payload, cancellationToken);
}
