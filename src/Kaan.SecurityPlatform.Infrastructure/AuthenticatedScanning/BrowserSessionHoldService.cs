using System.Collections.Concurrent;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Microsoft.Playwright;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class HeldBrowserSession : IAsyncDisposable
{
    public required Guid RunId { get; init; }
    public required IPlaywright Playwright { get; init; }
    public required IBrowser Browser { get; init; }
    public required IBrowserContext Context { get; init; }
    public required IPage Page { get; init; }
    public DateTime HeldAtUtc { get; init; } = DateTime.UtcNow;

    public async ValueTask DisposeAsync()
    {
        try { await Context.CloseAsync(); } catch { /* ignore */ }
        try { await Browser.CloseAsync(); } catch { /* ignore */ }
        try { Playwright.Dispose(); } catch { /* ignore */ }
    }
}

public sealed class BrowserSessionHoldService : IBrowserSessionHoldService
{
    private readonly ConcurrentDictionary<Guid, HeldBrowserSession> _sessions = new();

    public void Hold(Guid runId, HeldBrowserSession session)
    {
        if (_sessions.TryRemove(runId, out var previous))
        {
            _ = previous.DisposeAsync();
        }

        _sessions[runId] = session;
    }

    public bool TryGet(Guid runId, out HeldBrowserSession? session) =>
        _sessions.TryGetValue(runId, out session);

    public bool IsHeld(Guid runId) => _sessions.ContainsKey(runId);

    public async Task ReleaseAsync(Guid runId)
    {
        if (_sessions.TryRemove(runId, out var session))
        {
            await session.DisposeAsync();
        }
    }
}
