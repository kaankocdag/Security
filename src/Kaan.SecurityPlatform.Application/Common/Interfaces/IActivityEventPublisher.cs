namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

/// <summary>
/// Kullanıcı ve admin aktivite konsol widget'ına canlı olay yayınlamak için
/// kullanılan servis. Infrastructure katmanında SignalR üzerinde implemente edilir.
/// </summary>
public interface IActivityEventPublisher
{
    Task PublishToUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default);

    Task PublishToCompanyAsync(Guid companyId, string eventType, object payload, CancellationToken cancellationToken = default);

    Task PublishToSystemAdminsAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
