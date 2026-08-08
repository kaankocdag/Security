using System.Net;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

/// <summary>
/// Pasif tarayıcının hedeflenen URL veya IP adresinin SSRF, iç ağ ve
/// cloud metadata gibi tehlikeli hedeflerden korunmasını sağlayan sözleşme.
/// </summary>
public interface ITargetSafetyValidator
{
    TargetSafetyResult ValidateUri(Uri uri);
    TargetSafetyResult ValidateHost(string host);
    TargetSafetyResult ValidateResolvedIp(IPAddress address);
    Task<TargetSafetyResult> ValidateAndResolveAsync(Uri uri, CancellationToken cancellationToken = default);
}

public sealed record TargetSafetyResult(bool IsSafe, string? ReasonCode = null, string? Detail = null)
{
    public static TargetSafetyResult Safe() => new(true);
    public static TargetSafetyResult Unsafe(string reasonCode, string detail) => new(false, reasonCode, detail);
}
