using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning;

public sealed class AssessmentModeGuard : IAssessmentModeGuard
{
    public Result EnsureSupported(AssessmentMode mode)
    {
        return mode switch
        {
            AssessmentMode.PublicPassiveAssessment => Result.Success(),
            AssessmentMode.IsolatedSecurityLab => Result.Success(),
            AssessmentMode.AuthorizedExternalAssessment => Result.Success(),
            AssessmentMode.ApplicationSecurityCandidate => Result.Success(),
            _ => Result.Failure(
                "assessment_mode_forbidden",
                "Bu değerlendirme modu desteklenmiyor. Desteklenenler: PublicPassiveAssessment, IsolatedSecurityLab, AuthorizedExternalAssessment, ApplicationSecurityCandidate.")
        };
    }

    public Result EnsureNameAllowed(string? modeName)
    {
        if (string.IsNullOrWhiteSpace(modeName))
        {
            return Result.Success();
        }

        if (AssessmentModeNames.ForbiddenForever.Any(f =>
                string.Equals(f, modeName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(
                "assessment_mode_forbidden",
                $"Bu değerlendirme modu adı desteklenmiyor: {modeName}.");
        }

        if (!AssessmentModeNames.Supported.Any(s =>
                string.Equals(s, modeName.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(
                "assessment_mode_unknown",
                $"Bilinmeyen değerlendirme modu: {modeName}. Desteklenenler: {string.Join(", ", AssessmentModeNames.Supported)}");
        }

        return Result.Success();
    }

    public Result EnsureEnvironmentAllows(AssessmentMode mode, string environmentName)
    {
        return EnsureSupported(mode);
    }
}
