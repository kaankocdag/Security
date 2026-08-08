using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Lab;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabStartRequestGuard : ILabStartRequestGuard
{
    public Result ValidateNoForbiddenFields(IDictionary<string, object?> rawFields)
    {
        foreach (var key in rawFields.Keys)
        {
            if (LabConstants.ForbiddenRequestFields.Any(f =>
                    string.Equals(f, key, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure(
                    "lab_forbidden_field",
                    $"'{key}' alanı laboratuvar API'sinde kabul edilmez. Yalnızca kayıtlı senaryo anahtarı kullanılabilir.");
            }
        }

        return Result.Success();
    }
}
