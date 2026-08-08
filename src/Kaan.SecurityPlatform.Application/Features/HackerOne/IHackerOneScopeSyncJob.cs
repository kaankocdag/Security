namespace Kaan.SecurityPlatform.Application.Features.HackerOne;

/// <summary>Hangfire job: all HackerOne program scopes → Domains.</summary>
public interface IHackerOneScopeSyncJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
