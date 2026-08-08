using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

/// <summary>
/// SignalR henüz kaydedilmediği bağlamlar için (Worker ve tasarım zamanı) varsayılan yayıncı.
/// Sadece log satırı yazar. API katmanı gerçek SignalR uygulaması ile bunu override eder.
/// </summary>
public sealed class NoopActivityEventPublisher : IActivityEventPublisher
{
    private readonly ILogger<NoopActivityEventPublisher> _logger;

    public NoopActivityEventPublisher(ILogger<NoopActivityEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishToUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activity(user:{UserId} {EventType}) yayınlandı", userId, eventType);
        return Task.CompletedTask;
    }

    public Task PublishToCompanyAsync(Guid companyId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activity(company:{CompanyId} {EventType}) yayınlandı", companyId, eventType);
        return Task.CompletedTask;
    }

    public Task PublishToSystemAdminsAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Activity(sysadmin {EventType}) yayınlandı", eventType);
        return Task.CompletedTask;
    }
}
