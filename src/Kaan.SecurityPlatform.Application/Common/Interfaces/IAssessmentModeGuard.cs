using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IAssessmentModeGuard
{
    Result EnsureSupported(AssessmentMode mode);

    /// <summary>
    /// İsim bazlı reddetme (bilinmeyen / yasaklı alternatif adlar).
    /// </summary>
    Result EnsureNameAllowed(string? modeName);

    /// <summary>
    /// Ortam bazlı kısıt (üç desteklenen mod tüm ortamlarda serbest).
    /// </summary>
    Result EnsureEnvironmentAllows(AssessmentMode mode, string environmentName);
}
