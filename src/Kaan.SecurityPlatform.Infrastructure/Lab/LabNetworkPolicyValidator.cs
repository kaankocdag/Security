using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Common.Models;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabNetworkPolicyValidator : ILabNetworkPolicyValidator
{
    public Result ValidateTarget(
        Guid executionId,
        string? requestedTarget,
        string? allowedInternalEndpoint,
        string? allowedExternalHost = null)
    {
        if (string.IsNullOrWhiteSpace(requestedTarget))
        {
            return Result.Success();
        }

        if (!Uri.TryCreate(requestedTarget, UriKind.Absolute, out var requested))
        {
            return Result.Failure("lab_target_rejected", "Lab hedefi geçersiz.");
        }

        if (!string.IsNullOrWhiteSpace(allowedInternalEndpoint) &&
            Uri.TryCreate(allowedInternalEndpoint, UriKind.Absolute, out var allowedInternal) &&
            string.Equals(allowedInternal.Host, requested.Host, StringComparison.OrdinalIgnoreCase) &&
            allowedInternal.Port == requested.Port)
        {
            return Result.Success();
        }

        if (!string.IsNullOrWhiteSpace(allowedExternalHost) &&
            string.Equals(allowedExternalHost.Trim(), requested.Host, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Success();
        }

        return Result.Failure(
            "lab_target_rejected",
            $"Hedef, execution {executionId:N} için allowlist'teki lab hedefi veya iç uç nokta değil.");
    }
}
